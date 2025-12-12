using System;
using System.Threading;
using Emoki.Platforms.Windows;
using Emoki.Core;
using System.Collections.Generic;
using System.Linq; 

namespace Emoki
{
    class Program
    {
        private static Dictionary<string, string> _emojiShortcutMap = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            _emojiShortcutMap = new EmojiDatabase().GetAll();

            KeyboardHook.OnBufferChanged += (buffer) =>
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', Console.BufferWidth));
                Console.SetCursorPosition(0, Console.CursorTop);
                
                string mode = buffer.Contains(':') ? $"UNLIMITED (Max 16)" : "LIMITED (Max 5)";
                string outputLine = $"Buffer Mode: {mode} | Current Buffer: {buffer}";

                if (buffer.Contains(':'))
                {
                    int lastColonIndex = buffer.LastIndexOf(':');
                    string query = buffer.Substring(lastColonIndex);
                    
                    if (query.Length > 1 && query[1] != ' ')
                    {
                        var results = EmojiSearch.Search(_emojiShortcutMap, query);
                        
                        if (results.Count > 0) 
                        {
                            var selectedEmoji = results.First();
                            outputLine += $" | SELECTED: {selectedEmoji.Key} → {selectedEmoji.Value}";

                            outputLine += " | MATCHES: ";
                            string matchDisplay = string.Join(" | ", results.Select(kvp => $"{kvp.Key}→{kvp.Value}"));
                            outputLine += matchDisplay;
                        }
                    }
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

            Console.WriteLine("Keyboard Hook Started.");
            Console.WriteLine("Type any characters. Limited mode (Max 5) until you type a colon (:).");
            Console.WriteLine("Typing a character immediately after ':' will trigger the search.");
            Console.WriteLine("The first match will be displayed as 'SELECTED'.");
            Console.WriteLine("Reset Triggers: Backspace on ':', Length > 16, or Any Mouse Click.");
            Console.WriteLine("\nPress Enter to exit...");
            Console.ReadLine();

            KeyboardHook.Stop();
        }
    }
}