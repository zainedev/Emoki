using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Emoki.Core;

namespace Emoki.Platforms.Windows
{
    public static class KeyboardHook
    {
        // --- Keyboard Constants ---
        // Low-level keyboard hook id
        private const int WH_KEYBOARD_LL = 13;
        // Window messages we care about
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // Virtual-key codes used by the hook logic
        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt
        private const int VK_BACK = 0x08; // Backspace
        private const int VK_RETURN = 0x0D; // Enter
        private const int VK_TAB = 0x09; // Tab

        // --- Mouse Constants ---
        // Low-level mouse hook id
        private const int WH_MOUSE_LL = 14;
        // Mouse messages used to detect clicks
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        // --- Buffer Constants and Fields ---
        // Internal rolling character buffer captured from global keyboard events
        private static string _characterBuffer = string.Empty;
        // In limited mode we keep only the last N characters
        private const int BUFFER_MAX_LENGTH = 5;
        // When a colon is typed we allow a longer 'unlimited' capture up to this cap
        private const int UNLIMITED_MODE_MAX_LENGTH = 16;

        // --- Hook Management ---
        // Hook identifiers returned by SetWindowsHookEx
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;

        // Delegates kept alive to avoid GC collection while hooks are active
        private static LowLevelKeyboardProc _keyboardProc = HookCallback;
        private static LowLevelMouseProc _mouseProc = MouseHookCallback;

        // Event raised when the buffer changes (subscribers run on hook thread)
        public static event Action<string>? OnBufferChanged;

        // --- Popup interaction helpers ---
        // If set, invoked on Enter; return true to suppress the Enter key
        public static Func<bool>? OnEnterPressed;
        // Cached current buffer length for quick checks from UI
        public static int CurrentBufferLength { get; private set; } = 0;

        // HWND of the popup window (used to avoid resetting buffer on popup clicks)
        public static IntPtr PopupHandle { get; set; } = IntPtr.Zero;
        
        // Clear internal buffer and notify subscribers
        public static void ClearBuffer()
        {
            _characterBuffer = string.Empty;
            CurrentBufferLength = 0;
            OnBufferChanged?.Invoke(_characterBuffer);
        }

        // Install hooks and run a message loop on this thread to keep hooks alive
        public static void Start()
        {
            _keyboardHookID = SetHook(WH_KEYBOARD_LL, _keyboardProc);
            _mouseHookID = SetHook(WH_MOUSE_LL, _mouseProc);

            // Simple message pump required for low-level hooks to function
            MSG msg = new MSG();
            while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        
        // Remove installed hooks. Note: caller should ensure message pump exits.
        public static void Stop()
        {
            if (_keyboardHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHookID);
                _keyboardHookID = IntPtr.Zero;
            }

            if (_mouseHookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookID);
                _mouseHookID = IntPtr.Zero;
            }
        }

        private static IntPtr SetHook(int idHook, Delegate proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule!;
            return SetWindowsHookEx(idHook, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
        
        private static void ResetBuffer()
        {
            if (_characterBuffer != string.Empty)
            {
                _characterBuffer = string.Empty;
                CurrentBufferLength = 0;
                OnBufferChanged?.Invoke(_characterBuffer);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Low-level keyboard hook callback invoked on the hook thread.
        // Processes key events, updates the rolling buffer, and raises events.
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool bufferChanged = false;

                // If an Enter handler is defined, invoke it and suppress if requested
                if (vkCode == VK_RETURN)
                {
                    try
                    {
                        if (OnEnterPressed != null && OnEnterPressed.Invoke())
                        {
                            return (IntPtr)1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[KeyboardHook] OnEnterPressed threw: {ex}");
                    }
                }

                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                bool altPressed = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;

                // Backspace: remove last char from buffer
                if (vkCode == VK_BACK)
                {
                    if (_characterBuffer.Length > 0)
                    {
                        _characterBuffer = _characterBuffer.Substring(0, _characterBuffer.Length - 1);
                        bufferChanged = true;
                    }
                }
                // Enter/Tab: usually treated as a space unless part of modifier combos
                else if (vkCode == VK_RETURN || vkCode == VK_TAB)
                {
                    if (!ctrlPressed && !altPressed)
                    {
                        int colonIndex = _characterBuffer.LastIndexOf(':');
                        bool isSpaceAfterColon = colonIndex != -1 && colonIndex == _characterBuffer.Length - 1;

                        if (!isSpaceAfterColon)
                        {
                            _characterBuffer += " ";
                            bufferChanged = true;
                        }
                    }
                }
                // Printable key ranges (letters, punctuation, numpad)
                else if ((vkCode >= 0x30 && vkCode <= 0x5A) || (vkCode >= 0xBA && vkCode <= 0xC0) ||
                         (vkCode >= 0xDB && vkCode <= 0xDF) || (vkCode >= 0x60 && vkCode <= 0x6F) || (vkCode == 0x20))
                {
                    if (!ctrlPressed && !altPressed)
                    {
                        byte[] keyboardState = new byte[256];
                        if (shiftPressed) keyboardState[VK_SHIFT] = 0x80;

                        byte[] charBuffer = new byte[2];
                        int result = ToAscii(vkCode, Marshal.ReadInt32(lParam), keyboardState, charBuffer, 0);

                        if (result == 1)
                        {
                            char c = (char)charBuffer[0];
                            if (c >= 0x20)
                            {
                                _characterBuffer += c;
                                bufferChanged = true;
                            }
                        }
                    }
                }

                // If the buffer changed, enforce length rules and notify subscribers
                if (bufferChanged)
                {
                    bool wasBufferUnlimited = _characterBuffer.Contains(':');
                    if (wasBufferUnlimited && _characterBuffer.Length > UNLIMITED_MODE_MAX_LENGTH)
                    {
                        _characterBuffer = string.Empty;
                    }

                    bool isBufferUnlimitedAfterChange = _characterBuffer.Contains(':');
                    if (!isBufferUnlimitedAfterChange && _characterBuffer.Length > BUFFER_MAX_LENGTH)
                    {
                        _characterBuffer = _characterBuffer.Substring(_characterBuffer.Length - BUFFER_MAX_LENGTH);
                    }
                    else if (!isBufferUnlimitedAfterChange && wasBufferUnlimited)
                    {
                        _characterBuffer = string.Empty;
                    }

                    CurrentBufferLength = _characterBuffer.Length;
                    try
                    {
                        OnBufferChanged?.Invoke(_characterBuffer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[KeyboardHook] OnBufferChanged threw: {ex}");
                    }
                }
            }

            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Low-level mouse hook callback: resets buffer when clicking outside popup
        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN ||
                    wParam == (IntPtr)WM_RBUTTONDOWN ||
                    wParam == (IntPtr)WM_MBUTTONDOWN)
                {
                    // 1. Marshal mouse hook struct and get cursor point
                    MSLLHOOKSTRUCT mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                    // 2. Find the window under the cursor
                    IntPtr clickedWindowHandle = WindowFromPoint(mouseStruct.pt);

                    // 3. Only reset the buffer if click was outside our popup window
                    if (clickedWindowHandle != PopupHandle && GetParent(clickedWindowHandle) != PopupHandle)
                    {
                        ResetBuffer();
                    }
                }
            }

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        #region Win32 API
        // Mouse hook structure received from the OS (points in screen coordinates)
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT {
            public System.Drawing.Point pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(System.Drawing.Point point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        // Message structure used by the message pump on the hook thread
        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point pt;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpKeyState, [Out] byte[] lpChar, int uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);
        #endregion
    }
}