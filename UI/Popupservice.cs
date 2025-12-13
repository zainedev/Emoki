using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Emoki.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Threading; // Added for Dispatcher

namespace Emoki.UI
{
    // Simple structure to hold result data for the view model
    public record PopupResult(string Shortcut, string Emoji);
    
    public class PopupService
    {
        private PopupView? _popupWindow;

        // METHOD: Ensures the window is created and its native handle is materialized at startup.
        public void InitializeWindowHandle()
        {
             if (_popupWindow == null)
            {
                // Instantiate the PopupView. The constructor sets 'ShowActivated = false' and 'Topmost = true'.
                _popupWindow = new PopupView();
                _popupWindow.CanResize = false;
                _popupWindow.SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight;
                _popupWindow.IsVisible = false; 

                // CRITICAL FIX 1: Call Show() to force the creation of the native window handle (HWND).
                _popupWindow.Show();

                // CRITICAL FIX 2: Explicitly hide the window on the UI thread immediately after Show().
                Dispatcher.UIThread.Post(() =>
                {
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

            // Get the current cursor screen position
            var screenPosition = GetCurrentCursorPosition();
            
            // Safety check: ensure the window is initialized.
            if (_popupWindow == null)
            {
                InitializeWindowHandle();
            }
            
            // Set the ViewModel data
            var viewModel = (PopupViewModel)_popupWindow!.DataContext!; 
            viewModel.UpdateResults(searchResults.Select(kvp => new PopupResult(kvp.Key, kvp.Value)).ToList());
            
            // Position the window (must be done before showing it)
            _popupWindow.Position = new PixelPoint((int)screenPosition.X, (int)screenPosition.Y);
            
            if (!_popupWindow.IsVisible)
            {
                // Standard Show() call, which now respects the ShowActivated = false setting.
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
        
        // This is a placeholder for getting the actual cursor position.
        private PixelPoint GetCurrentCursorPosition()
        {
            // Placeholder: Returns a fixed position near the center/top of the screen.
            return new PixelPoint(1000, 500); 
        }
    }
}