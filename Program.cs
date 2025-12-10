using System;
using System.Threading;
using Emoki.Platforms.Windows;

namespace Emoki
{
    class Program
    {
        static void Main(string[] args)
        {
            // Subscribe to the Shift+: event
            KeyboardHook.OnShiftColonPressed += () =>
            {
                Console.WriteLine("key pressed in the console");
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

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();

            // Stop the hook before exiting
            KeyboardHook.Stop();
        }
    }
}
