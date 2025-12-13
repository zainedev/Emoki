using Avalonia;
using System;
using System.Collections.Generic;
using Emoki.Core;
using Emoki.UI;
using Avalonia.Threading; // Crucial for calling UI operations from a background thread
using System.Text;

namespace Emoki
{
    class Program
    {
        public static Dictionary<string, string> EmojiShortcutMap { get; private set; } = new Dictionary<string, string>();
        
        // This must be initialized after the Dispatcher is running, but we'll instantiate it here.
        private static PopupService _popupService = new PopupService(); 

        [STAThread]
        public static void Main(string[] args)
        {
            // 1. Load the database before the UI starts
            EmojiShortcutMap = new EmojiDatabase().GetAll();
            Console.WriteLine($"Loaded {EmojiShortcutMap.Count} emoji shortcuts. Starting UI and Hook...");

            // 2. Subscribe to the keyboard hook event.
            Emoki.Platforms.Windows.KeyboardHook.OnBufferChanged += HandleBufferChanged;

            // 3. Start the keyboard hook on a separate background thread
            System.Threading.Thread hookThread = new System.Threading.Thread(() =>
            {
                Emoki.Platforms.Windows.KeyboardHook.Start();
            })
            {
                IsBackground = true
            };
            hookThread.Start();
            
            // 4. Initialize and run the Avalonia application
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            
            // Note: Control never reaches here until the app is closed.
        }

        private static void HandleBufferChanged(string buffer)
        {
            // All UI operations MUST be run on the main UI thread (Dispatcher).
            Dispatcher.UIThread.Invoke(() =>
            {
                try
                {
                    // Console output is removed here to prevent conflicts.
                    
                    List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();
                    bool searchTriggered = false;

                    if (buffer.Contains(':'))
                    {
                        int lastColonIndex = buffer.LastIndexOf(':');
                        string rawToken = buffer.Substring(lastColonIndex);

                        // Sanitization and token extraction logic
                        StringBuilder clean = new StringBuilder();
                        foreach (char c in rawToken)
                        {
                            if (c == ':' || char.IsLetterOrDigit(c) || c == '_' || c == '-')
                                clean.Append(char.ToLowerInvariant(c));
                        }
                        string token = clean.ToString();

                        if (token.Length > 1)
                        {
                            results = EmojiSearch.Search(EmojiShortcutMap, token);
                            searchTriggered = true;
                        }
                    }
                    
                    if (searchTriggered && results.Count > 0)
                    {
                        _popupService.ShowPopup(results);
                    }
                    else
                    {
                        _popupService.HidePopup();
                    }
                }
                catch (Exception ex)
                {
                    // Print any exception on the UI thread to the console for debugging
                    Console.WriteLine($"[FATAL UI ERROR] {ex.Message}");
                    _popupService.HidePopup();
                }
            });
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}