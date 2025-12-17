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
    class Program
    {
        public static Dictionary<string, string> EmojiShortcutMap { get; private set; } = new Dictionary<string, string>();
        private static PopupService _popupService = new PopupService(); 
        private static KeyValuePair<string, string>? _activeMatch = null; 
        private static string _currentRawBuffer = string.Empty; 

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

                // 4. Mouse Click Injection
                PopupService.OnEmojiSelected = (result) => 
                {
                    PerformInjection(result.Emoji, _currentRawBuffer);
                };

                // 5. Start Hook Thread
                System.Threading.Thread hookThread = new System.Threading.Thread(() =>
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
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                // 4. Wait for the OS to finalize the focus shift (Critical for Mouse Clicks)
                System.Threading.Thread.Sleep(60); 

                // 5. Erase and Inject
                TextInjector.Erase(shortcutLengthToErase);
                TextInjector.InjectText(emojiToInject);
                
                // 6. Final cleanup
                _activeMatch = null;
                KeyboardHook.ClearBuffer(); 
            });
        }

        private static bool HandleEnterKeySuppression()
        {
            if (_activeMatch.HasValue)
            {
                PerformInjection(_activeMatch.Value.Value, _currentRawBuffer);
                return true; 
            }
            return false;
        }

        private static int GetShortcutLength(string buffer)
        {
            if (string.IsNullOrEmpty(buffer)) return 0;
            int lastColonIndex = buffer.LastIndexOf(':');
            if (lastColonIndex == -1) return 0;
            return buffer.Length - lastColonIndex;
        }

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
                        _activeMatch = results.First(); 
                        _popupService.ShowPopup(results);
                    }
                    else
                    {
                        _activeMatch = null;
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

        public static void InitializePopupService() => _popupService.InitializeWindowHandle();

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}