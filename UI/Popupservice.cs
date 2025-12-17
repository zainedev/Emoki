using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Emoki.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Emoki.UI
{
    public record PopupResult(string Shortcut, string Emoji);
    
    public class PopupService
    {
        private PopupView? _popupWindow;
        public static Action<PopupResult>? OnEmojiSelected;
        
        public void InitializeWindowHandle()
        {
            if (_popupWindow == null)
            {
                _popupWindow = new PopupView();
                _popupWindow.CanResize = false;
                _popupWindow.SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight;
                _popupWindow.IsVisible = false; 

                _popupWindow.Show();

                var platformHandle = _popupWindow.TryGetPlatformHandle();
                if (platformHandle != null)
                {
                    Emoki.Platforms.Windows.KeyboardHook.PopupHandle = platformHandle.Handle;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    if (_popupWindow != null)
                        _popupWindow.IsVisible = false;
                });
            }
        }

        public void ShowPopup(List<KeyValuePair<string, string>> searchResults)
        {
            if (searchResults == null || !searchResults.Any())
            {
                HidePopup();
                return;
            }

            var screenPosition = GetCurrentCursorPosition();
            
            if (_popupWindow == null)
            {
                InitializeWindowHandle();
            }
            
            var viewModel = (PopupViewModel)_popupWindow!.DataContext!; 
            viewModel.UpdateResults(searchResults.Select(kvp => new PopupResult(kvp.Key, kvp.Value)).ToList());
            
            _popupWindow.Position = new PixelPoint((int)screenPosition.X, (int)screenPosition.Y);
            
            if (!_popupWindow.IsVisible)
            {
                _popupWindow.Show();
            }
        }

        public void HidePopup()
        {
            if (_popupWindow != null)
            {
                _popupWindow.IsVisible = false;
            }
        }
        
        private PixelPoint GetCurrentCursorPosition()
        {
            // Placeholder: Replace with real Win32 cursor logic if needed
            return new PixelPoint(1000, 500); 
        }
    }
}