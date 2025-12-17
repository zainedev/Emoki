using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Emoki.Platforms.Windows
{
    public static class TextInjector
    {
        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const byte VK_BACK = 0x08;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT { public int dx, dy, mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT { public int uMsg; public ushort wParamL, wParamH; }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void Erase(int length)
        {
            if (length <= 0) return;
            
            INPUT[] inputs = new INPUT[length * 2]; 

            for (int i = 0; i < length; i++)
            {
                // Key Down
                inputs[i * 2] = new INPUT {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_BACK, dwFlags = 0 } }
                };
                
                // Key Up
                inputs[i * 2 + 1] = new INPUT {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wVk = VK_BACK, dwFlags = KEYEVENTF_KEYUP } }
                };
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        public static void InjectText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            // Short delay to ensure backspaces are fully processed by the target app buffer
            Thread.Sleep(20); 

            INPUT[] inputs = new INPUT[text.Length * 2];
            
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];

                inputs[i * 2] = new INPUT {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE } }
                };

                inputs[i * 2 + 1] = new INPUT {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } }
                };
            }

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }
}