using System;
using System.Threading;
using Emoki.Platforms.Windows;
using Emoki.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emoki
{
    class Program
    {
        private static Dictionary<string, string> _emojiShortcutMap = new Dictionary<string, string>();
        private const int MAX_CONSOLE_OUTPUT_LENGTH = 120; // Limit output length to prevent wrapping issues

        static void Main(string[] args)
        {
            _emojiShortcutMap = new EmojiDatabase().GetAll();

            Console.WriteLine($"Loaded {_emojiShortcutMap.Count} emoji shortcuts.");

            KeyboardHook.OnBufferChanged += (buffer) =>
            {
                // --- Start Clear Logic ---
                // Save current cursor position, clear the line, and reset cursor
                int currentLine = Console.CursorTop;
                Console.SetCursorPosition(0, currentLine);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, currentLine);
                // --- End Clear Logic ---


                string outputLine = $"Raw Buffer: [{buffer}]";

                int colonIndex = buffer.LastIndexOf(':');
                if (colonIndex != -1)
                {
                    string rawToken = buffer.Substring(colonIndex);

                    StringBuilder clean = new StringBuilder();
                    foreach (char c in rawToken)
                    {
                        // Ensure only valid shortcut characters are processed
                        if (c == ':' || char.IsLetterOrDigit(c) || c == '_' || c == '-')
                            clean.Append(char.ToLowerInvariant(c));
                    }

                    string token = clean.ToString();
                    outputLine += $" | Clean Token: [{token}]";

                    if (token.Length > 1)
                    {
                        var results = EmojiSearch.Search(_emojiShortcutMap, token);

                        if (results.Count > 0)
                        {
                            var selected = results[0];
                            outputLine += $" | SELECTED: {selected.Key} → {selected.Value}";
                            
                            // Build the MATCHES string
                            string matchString = string.Join(
                                " | ",
                                results.Select(kvp => $"{kvp.Key}→{kvp.Value}")
                            );
                            
                            outputLine += " | MATCHES: " + matchString;
                        }
                        else
                        {
                            outputLine += " | NO MATCHES";
                        }
                    }
                }
                
                // Truncate the final output string to prevent wrapping and clutter
                if (outputLine.Length > MAX_CONSOLE_OUTPUT_LENGTH)
                {
                    outputLine = outputLine.Substring(0, MAX_CONSOLE_OUTPUT_LENGTH - 3) + "...";
                }

                Console.Write(outputLine);
            };

            Thread hookThread = new Thread(() =>
            {
                KeyboardHook.Start();
            })
            {
                IsBackground = true
            };
            hookThread.Start();

            Console.WriteLine("Emoki running (global keyboard hook active)");
            Console.WriteLine("Type ':' followed by letters anywhere.");
            Console.WriteLine("Press Enter here to exit.\n");

            Console.ReadLine();
            KeyboardHook.Stop();
        }
    }
}