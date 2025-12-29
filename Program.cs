using Avalonia;
using System;
using System.Collections.Generic;
using Emoki.Core;
using Emoki.UI;
using Avalonia.Threading;
using System.Text;
using System.Linq;
using Emoki.Platforms.Windows;

namespace Emoki
{
    // Application bootstrap and coordination
    class Program
    {
        // Emoji lookup table loaded from the database: shortcut -> emoji
        public static Dictionary<string, string> EmojiShortcutMap { get; private set; } = new Dictionary<string, string>();

        // Manages the popup window lifecycle and interactions
        private static PopupService _popupService = new PopupService();

        // Latest raw character buffer snapshot reported by the keyboard hook
        private static string _currentRawBuffer = string.Empty;

        // Program entry: set up handlers, load DB, start keyboard hook and UI lifecycle.
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Console.WriteLine($"[UNHANDLED] {e.ExceptionObject}");

            try
            {
                // 1. Load Data
                EmojiShortcutMap = new EmojiDatabase().GetAll();

                // 2. Build App
                var appBuilder = BuildAvaloniaApp();

                // 3. Hooks
                KeyboardHook.OnBufferChanged += HandleBufferChanged;
                KeyboardHook.OnEnterPressed = HandleEnterKeySuppression;
                KeyboardHook.OnUpPressed = HandleUpKeySuppression;
                KeyboardHook.OnDownPressed = HandleDownKeySuppression;

                // 4. Mouse Click Injection
                PopupService.OnEmojiSelected = (result) => 
                {
                    PerformInjection(result.Emoji, _currentRawBuffer);
                };

                // 5. Start Hook Thread
                Thread hookThread = new Thread(() =>
                {
                    KeyboardHook.Start();
                }) { IsBackground = true };
                hookThread.Start();

                // 6. Run UI
                appBuilder.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] {ex}");
                Environment.Exit(1);
            }
        }

        // PerformInjection: erases the typed shortcut and injects the selected emoji.
        // - `emojiToInject`: the emoji character/string to insert
        // - `bufferSnapshot`: snapshot of the hook buffer used to compute erase length
        private static void PerformInjection(string emojiToInject, string bufferSnapshot)
        {
            // 1. Calculate length immediately from the snapshot
            int shortcutLengthToErase = GetShortcutLength(bufferSnapshot);
            if (shortcutLengthToErase < 2) return;

            // 2. Hide the popup first to trigger focus restoration to the previous app
            Dispatcher.UIThread.Invoke(() =>
            {
                _popupService.HidePopup();
            });

            // 3. Execute injection on background thread to not freeze UI
            ThreadPool.QueueUserWorkItem(_ =>
            {
                // 4. Wait for the OS to finalize the focus shift (Critical for Mouse Clicks)
                Thread.Sleep(60); 

                // 5. Erase and Inject
                TextInjector.Erase(shortcutLengthToErase);
                TextInjector.InjectText(emojiToInject);
                
                // 6. Final cleanup
                KeyboardHook.ClearBuffer(); 
            });
        }

        // HandleEnterKeySuppression: called by the hook when Enter is pressed.
        // Returns true to suppress the physical Enter key, false to allow it.
        private static bool HandleEnterKeySuppression()
        {
            var active = _popupService.GetActiveSelection();
            if (active != null)
            {
                PerformInjection(active.Emoji, _currentRawBuffer);
                return true;
            }
            return false;
        }

        // HandleUpKeySuppression: called by the hook when Up is pressed.
        // Returns true to suppress the physical Up key when popup is active.
        private static bool HandleUpKeySuppression()
        {
            var active = _popupService.GetActiveSelection();
            if (active != null)
            {
                Dispatcher.UIThread.Invoke(() => _popupService.MoveSelectionUp());
                return true;
            }
            return false;
        }

        // HandleDownKeySuppression: called by the hook when Down is pressed.
        // Returns true to suppress the physical Down key when popup is active.
        private static bool HandleDownKeySuppression()
        {
            var active = _popupService.GetActiveSelection();
            if (active != null)
            {
                Dispatcher.UIThread.Invoke(() => _popupService.MoveSelectionDown());
                return true;
            }
            return false;
        }

        // GetShortcutLength: returns number of characters (including colon)
        // from the last colon to buffer end. Used for backspace count.
        private static int GetShortcutLength(string buffer)
        {
            if (string.IsNullOrEmpty(buffer)) return 0;
            int lastColonIndex = buffer.LastIndexOf(':');
            if (lastColonIndex == -1) return 0;
            return buffer.Length - lastColonIndex;
        }

        // HandleBufferChanged: runs on UI dispatcher to sanitize token,
        // call search, and show/hide popup accordingly.
        private static void HandleBufferChanged(string buffer)
        {
            _currentRawBuffer = buffer; 

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
                    Console.WriteLine($"[UI ERROR] {ex.Message}");
                    _popupService.HidePopup();
                }
            });
        }

        // InitializePopupService: helper invoked by Avalonia `App` after platform
        // initialization to create the (hidden) popup window handle.
        public static void InitializePopupService() => _popupService.InitializeWindowHandle();

        // BuildAvaloniaApp: avalonia app builder used to start the UI lifetime.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}