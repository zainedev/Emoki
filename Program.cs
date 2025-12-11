using System;
using System.Threading;
using Emoki.Platforms.Windows;

namespace Emoki
{
    class Program
    {
        static void Main(string[] args)
        {
            // Subscribe to the buffer change event
            KeyboardHook.OnBufferChanged += (buffer) =>
            {
                // Clear the line and rewrite the buffer content
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, Console.CursorTop);
                
                string mode = buffer.Contains(':') ? $"UNLIMITED (Max 16)" : "LIMITED (Max 5)";
                Console.Write($"Buffer Mode: {mode} | Current Buffer: {buffer}");
            };

            // Start the keyboard hook on a separate thread
            Thread hookThread = new Thread(() =>
            {
                KeyboardHook.Start();
            })
            {
                IsBackground = true
            };
            hookThread.Start();

            Console.WriteLine("Keyboard Hook Started.");
            Console.WriteLine("Type any characters. Limited mode (Max 5) until you type a colon (:).");
            Console.WriteLine("UNLIMITED mode (Max 16) triggers a reset (buffer cleared) if length is exceeded.");
            Console.WriteLine("Reset Triggers: Backspace on ':', Length > 16, or **Any Mouse Click**.");
            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();

            // Stop the hook before exiting
            KeyboardHook.Stop();
        }
    }
}