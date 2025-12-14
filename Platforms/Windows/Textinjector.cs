using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Emoki.Platforms.Windows
{
    public static class TextInjector
    {
        // --- WIN32 P/INVOKE CONSTANTS AND STRUCTS ---
        
        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const byte VK_BACK = 0x08;
        
        // Define the correct union structure for stability
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
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            // Add offsets for Mouse and Hardware input to ensure struct size is correct
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
            // IMPORTANT: For 64-bit processes, the size of INPUT must be 40 bytes.
            // On 32-bit, it's 28. Using the explicit definition ensures it's correct.
        }

        // Placeholder structs to ensure correct union size
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT { public int dx, dy, mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT { public int uMsg; public ushort wParamL, wParamH; }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // --- PUBLIC INJECTION METHODS ---

        /// <summary>
        /// Simulates backspace key presses to erase the keyword shortcut.
        /// </summary>
        public static void Erase(int length)
        {
            if (length <= 0) return;
            
            // KeyDown and KeyUp for each backspace
            INPUT[] inputs = new INPUT[length * 2]; 

            for (int i = 0; i < length; i++)
            {
                // Key Down
                inputs[i * 2] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = VK_BACK,
                            dwFlags = 0, // Key Down
                        }
                    }
                };
                
                // Key Up
                inputs[i * 2 + 1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = VK_BACK,
                            dwFlags = KEYEVENTF_KEYUP, // Key Up
                        }
                    }
                };
            }
            
            // Send backspaces as a batch
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        /// <summary>
        /// Injects text (the emoji) into the current cursor position using Unicode events.
        /// </summary>
        public static void InjectText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            // CRITICAL FIX: Add a brief delay to allow the host application to process the Erase command.
            Thread.Sleep(5); 

            INPUT[] inputs = new INPUT[text.Length * 2];
            
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];

                // Key Down (using KEYEVENTF_UNICODE flag)
                inputs[i * 2] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wScan = character,
                            dwFlags = KEYEVENTF_UNICODE, // Key Down, Unicode flag
                        }
                    }
                };
                
                // Key Up (using KEYEVENTF_UNICODE flag)
                inputs[i * 2 + 1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wScan = character,
                            dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP, // Key Up, Unicode flag
                        }
                    }
                };
            }
            
            // Send the text injection as a batch
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }
}