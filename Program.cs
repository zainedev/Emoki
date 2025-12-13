using Avalonia;
using System;
using System.Collections.Generic;
using Emoki.Core;
using Emoki.UI;
using Avalonia.Threading;
using System.Text;
using System.Linq;

namespace Emoki
{
    class Program
    {
        public static Dictionary<string, string> EmojiShortcutMap { get; private set; } = new Dictionary<string, string>();
        
        // This must be instantiated here.
        private static PopupService _popupService = new PopupService(); 

        [STAThread]
        public static void Main(string[] args)
        {
            // Global exception handlers to capture crashes during startup/runtime
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"[UNHANDLED] {e.ExceptionObject}");
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine($"[UNOBSERVED TASK EX] {e.Exception}");
            };

            try
            {
                // 1. Load the database before the UI starts
                EmojiShortcutMap = new EmojiDatabase().GetAll();
                Console.WriteLine($"Loaded {EmojiShortcutMap.Count} emoji shortcuts. Starting UI and Hook...");
            
            // 2. Start the application life cycle first to get the Avalonia Dispatcher running.
            var appBuilder = BuildAvaloniaApp();
            
            // 3. PopupService initialization is deferred until Avalonia platform is ready.

            // 4. Subscribe to the keyboard hook event.
            Emoki.Platforms.Windows.KeyboardHook.OnBufferChanged += HandleBufferChanged;

            // 5. Start the keyboard hook on a separate background thread
            System.Threading.Thread hookThread = new System.Threading.Thread(() =>
            {
                Emoki.Platforms.Windows.KeyboardHook.Start();
            })
            {
                IsBackground = true
            };
            hookThread.Start();
            
                // 6. Run the Avalonia application
                appBuilder.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL STARTUP ERROR] {ex}");
                Environment.Exit(1);
            }
        }

        // Called by the Avalonia `App` once the framework is initialized on the UI thread.
        public static void InitializePopupService()
        {
            _popupService.InitializeWindowHandle();
        }

        private static void HandleBufferChanged(string buffer)
        {
            // All UI operations MUST be run on the main UI thread (Dispatcher).
            Dispatcher.UIThread.Invoke(() =>
            {
                try
                {
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