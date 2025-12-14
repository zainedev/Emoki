using Avalonia;
using System;
using System.Collections.Generic;
using Emoki.Core;
using Emoki.UI;
using Avalonia.Threading;
using System.Text;
using System.Linq;
using Emoki.Platforms.Windows; // Required for KeyboardHook and TextInjector

namespace Emoki
{
    class Program
    {
        public static Dictionary<string, string> EmojiShortcutMap { get; private set; } = new Dictionary<string, string>();
        
        private static PopupService _popupService = new PopupService(); 
        
        private static KeyValuePair<string, string>? _activeMatch = null; 
        
        // NEW FIELD: Store the raw buffer content for length calculation on Enter press.
        private static string _currentRawBuffer = string.Empty; 

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

                // 2. Build Avalonia application (do not start it yet)
                var appBuilder = BuildAvaloniaApp();

                // 3. Subscribe to the keyboard hook event.
                // NOTE: We MUST subscribe to OnBufferChanged before setting OnEnterPressed
                KeyboardHook.OnBufferChanged += HandleBufferChanged;

                // 4. Set the Enter key suppression handler
                KeyboardHook.OnEnterPressed = HandleEnterKeySuppression;

                // 5. Start the keyboard hook on a separate background thread
                System.Threading.Thread hookThread = new System.Threading.Thread(() =>
                {
                    KeyboardHook.Start();
                }) { IsBackground = true };
                hookThread.Start();

                // 6. Run the Avalonia application (this call blocks until shutdown)
                appBuilder.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL STARTUP ERROR] {ex}");
                Environment.Exit(1);
            }
        }
        
        // NEW METHOD: Calculates the length of the shortcut string (including the colon).
        private static int GetShortcutLength(string buffer)
        {
            if (string.IsNullOrEmpty(buffer))
                return 0;

            int lastColonIndex = buffer.LastIndexOf(':');
            
            if (lastColonIndex == -1)
                return 0; // No colon found

            // Length from colon to the end of the string, inclusive of the colon itself.
            // buffer.Length is the total length. lastColonIndex is 0-based index.
            // e.g., "word:so" -> length 7. index of : is 4. Length is 7 - 4 = 3.
            return buffer.Length - lastColonIndex;
        }


        // MODIFIED METHOD: Performs injection using the dynamically calculated length.
        private static bool HandleEnterKeySuppression()
        {
            if (_activeMatch.HasValue)
            {
                // CRITICAL CALCULATION: Determine the exact length of the shortcut segment.
                int shortcutLengthToErase = GetShortcutLength(_currentRawBuffer);

                // Check for safety, it should be at least 2 (':' + one char)
                if (shortcutLengthToErase < 2) 
                {
                    // This is an error state, treat as normal Enter
                    return false; 
                }

                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    // 1. Get the emoji to inject
                    string emojiToInject = _activeMatch.Value.Value; 
                    
                    // 2. Perform the injection sequence
                    // Injector performs the backspaces using the specific shortcut length
                    TextInjector.Erase(shortcutLengthToErase);
                    TextInjector.InjectText(emojiToInject);
                    
                    // 3. Reset state on the UI thread after injection is complete
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        _popupService.HidePopup();
                        _activeMatch = null;
                        // IMPORTANT: Clear the hook's internal buffer after successful injection
                        KeyboardHook.ClearBuffer(); 
                    });
                });
                
                // Suppress the original ENTER key press from reaching the application
                return true; 
            }
            
            // Allow the ENTER key press to proceed normally
            return false;
        }

        public static void InitializePopupService()
        {
            _popupService.InitializeWindowHandle();
        }

        // MODIFIED METHOD: Update the raw buffer content.
        private static void HandleBufferChanged(string buffer)
        {
            // Update the raw buffer field for the injection logic
            _currentRawBuffer = buffer; 

            // All UI operations MUST be run on the main UI thread (Dispatcher).
            Dispatcher.UIThread.Invoke(() =>
            {
                try
                {
                    // ... (existing search logic remains here) ...
                    
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
                        // Store the first matching emoji for injection
                        _activeMatch = results.First(); 
                        _popupService.ShowPopup(results);
                    }
                    else
                    {
                        // Clear the active match if no match is found or the buffer is cleared
                        _activeMatch = null;
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