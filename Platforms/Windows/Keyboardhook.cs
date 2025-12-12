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
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // Alt key
        private const int VK_BACK = 0x08; // Backspace
        private const int VK_RETURN = 0x0D; // Enter
        private const int VK_TAB = 0x09; // Tab
        private const int VK_OEM_1 = 0xBA; // ; key (used for colon : when Shift is down)
        // Removed: private const char COLON_CHAR = ':'; 
        // Using literal ':' for consistency with search prefix requirement

        // --- Mouse Constants ---
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        // --- Buffer Constants and Fields ---
        private static string _characterBuffer = string.Empty;
        private const int BUFFER_MAX_LENGTH = 5;
        private const int UNLIMITED_MODE_MAX_LENGTH = 16; 

        // --- Hook Management ---
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private static IntPtr _mouseHookID = IntPtr.Zero;
        
        private static LowLevelKeyboardProc _keyboardProc = HookCallback;
        private static LowLevelMouseProc _mouseProc = MouseHookCallback;

        public static event Action<string>? OnBufferChanged;

        public static void Start()
        {
            _keyboardHookID = SetHook(WH_KEYBOARD_LL, _keyboardProc);
            _mouseHookID = SetHook(WH_MOUSE_LL, _mouseProc);
            
            MSG msg = new MSG();
            while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        
        public static void Stop()
        {
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
        }

        private static IntPtr SetHook(int idHook, Delegate proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule!;
            return SetWindowsHookEx(idHook, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
        
        // Helper method to reset the buffer and notify
        private static void ResetBuffer()
        {
            if (_characterBuffer != string.Empty)
            {
                _characterBuffer = string.Empty;
                OnBufferChanged?.Invoke(_characterBuffer);
            }
        }

        // --- Keyboard Hook Implementation ---
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool bufferChanged = false;

                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                bool altPressed = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                
                
                // --- Handle Backspace ---
                if (vkCode == VK_BACK)
                {
                    if (_characterBuffer.Length > 0)
                    {
                        _characterBuffer = _characterBuffer.Substring(0, _characterBuffer.Length - 1);
                        bufferChanged = true;
                    }
                }
                // --- Handle Enter and Tab as Space (only if not a shortcut) ---
                else if (vkCode == VK_RETURN || vkCode == VK_TAB)
                {
                    if (!ctrlPressed && !altPressed)
                    {
                        // Check if the current buffer already contains the colon, and if the
                        // character right after the colon is a space. If so, don't add another space.
                        int colonIndex = _characterBuffer.LastIndexOf(':');
                        bool isSpaceAfterColon = colonIndex != -1 && colonIndex == _characterBuffer.Length - 1;
                        
                        if (!isSpaceAfterColon)
                        {
                            _characterBuffer += " ";
                            bufferChanged = true;
                        }
                    }
                }
                // --- Handle Printable Characters ---
                else if (
                    (vkCode >= 0x30 && vkCode <= 0x5A) ||
                    (vkCode >= 0xBA && vkCode <= 0xC0) ||
                    (vkCode >= 0xDB && vkCode <= 0xDF) ||
                    (vkCode >= 0x60 && vkCode <= 0x6F) ||
                    (vkCode == 0x20)
                )
                {
                    if (!ctrlPressed && !altPressed)
                    {
                        byte[] keyboardState = new byte[256];
                        if (shiftPressed)
                        {
                            keyboardState[VK_SHIFT] = 0x80;
                        }

                        IntPtr hkl = GetKeyboardLayout(0);
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

                // --- Manage Buffer and Event ---
                if (bufferChanged)
                {
                    // 1. Determine the mode BEFORE truncation/reset
                    bool wasBufferUnlimited = _characterBuffer.Contains(':');

                    if (wasBufferUnlimited)
                    {
                        // 2. Check for the reset condition: length exceeding 16
                        if (_characterBuffer.Length > UNLIMITED_MODE_MAX_LENGTH)
                        {
                            _characterBuffer = string.Empty;
                        }
                    }

                    // 3. Re-evaluate mode after potential reset
                    bool isBufferUnlimitedAfterChange = _characterBuffer.Contains(':');
                    
                    // 4. Truncate if we are in the LIMITED mode
                    if (!isBufferUnlimitedAfterChange && _characterBuffer.Length > BUFFER_MAX_LENGTH)
                    {
                        // Truncate down to 5
                        _characterBuffer = _characterBuffer.Substring(_characterBuffer.Length - BUFFER_MAX_LENGTH);
                    }
                    else if (!isBufferUnlimitedAfterChange && wasBufferUnlimited)
                    {
                        // Edge case: Colon was backed out, and the resulting string is shorter than 5. 
                        // We must reset the buffer completely to avoid corrupted pre-colon context.
                        _characterBuffer = string.Empty; 
                    }
                    
                    OnBufferChanged?.Invoke(_characterBuffer);
                }
            }

            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }


        // --- Mouse Hook Implementation ---
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN ||
                    wParam == (IntPtr)WM_RBUTTONDOWN ||
                    wParam == (IntPtr)WM_MBUTTONDOWN)
                {
                    ResetBuffer();
                }
            }

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }

        #region Win32 API
        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

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
        private static extern IntPtr SetWindowsHookEx(int idHook,
            Delegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

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