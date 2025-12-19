using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Emoki.Platforms.Windows
{
    public static class TextInjector
    {
        // Input types and flags used by SendInput
        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const byte VK_BACK = 0x08; // Virtual key for Backspace

        // Keyboard input structure used by SendInput
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;         // Virtual-key code
            public ushort wScan;       // Hardware scan code / Unicode char when using KEYEVENTF_UNICODE
            public uint dwFlags;       // Flags (KEYEVENTF_UNICODE, KEYEVENTF_KEYUP, etc.)
            public uint time;          // Timestamp for the event (0 = system)
            public IntPtr dwExtraInfo; // Extra application-defined data
        }

        // Input union for SendInput (keyboard/mouse/hardware)
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        // Wrapper input structure required by SendInput
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type; // INPUT_KEYBOARD etc.
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT { public int dx, dy, mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT { public int uMsg; public ushort wParamL, wParamH; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // Erase: sends Backspace key events `length` times to the active window.
        // Produces pairs of keydown/keyup events for each backspace.
        public static void Erase(int length)
        {
            if (length <= 0) return;

            INPUT[] inputs = new INPUT[length * 2];

            for (int i = 0; i < length; i++)
            {
                // Key down (Backspace)
                inputs[i * 2] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_BACK, dwFlags = 0 } }
                };

                // Key up (Backspace)
                inputs[i * 2 + 1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_BACK, dwFlags = KEYEVENTF_KEYUP } }
                };
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        // InjectText: sends Unicode key events for each character in `text`.
        // Uses KEYEVENTF_UNICODE so target app receives the proper Unicode characters (emoji supported).
        public static void InjectText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Small delay to allow the target application to process prior backspaces
            Thread.Sleep(20);

            INPUT[] inputs = new INPUT[text.Length * 2];

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];

                // Key down (Unicode character)
                inputs[i * 2] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE } }
                };

                // Key up (Unicode character)
                inputs[i * 2 + 1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } }
                };
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }
}