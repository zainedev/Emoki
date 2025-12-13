using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Emoki.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;

namespace Emoki.UI
{
    // Simple structure to hold result data for the view model
    public record PopupResult(string Shortcut, string Emoji);
    
    public class PopupService
    {
        private PopupView? _popupWindow;

        public void ShowPopup(List<KeyValuePair<string, string>> searchResults)
        {
            if (searchResults == null || !searchResults.Any())
            {
                HidePopup();
                return;
            }

            // Get the current cursor screen position
            var screenPosition = GetCurrentCursorPosition();
            
            if (_popupWindow == null)
            {
                // Instantiate the PopupView. The constructor handles 'ShowActivated = false'.
                _popupWindow = new PopupView();
                _popupWindow.CanResize = false;
                _popupWindow.SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight;
                _popupWindow.IsVisible = false;
            }
            
            // Set the ViewModel data
            var viewModel = (PopupViewModel)_popupWindow.DataContext!;
            viewModel.UpdateResults(searchResults.Select(kvp => new PopupResult(kvp.Key, kvp.Value)).ToList());
            
            // Position the window (must be done before showing it)
            _popupWindow.Position = new PixelPoint((int)screenPosition.X, (int)screenPosition.Y);
            
            if (!_popupWindow.IsVisible)
            {
                // Standard Show() call. Since PopupView.ShowActivated is false, it will not steal focus.
                _popupWindow.Show();
            }
            
            // Do NOT call _popupWindow.Activate();
        }

        public void HidePopup()
        {
            if (_popupWindow != null)
            {
                _popupWindow.IsVisible = false;
                // Note: We don't close/dispose the window here, allowing it to be reused for performance.
            }
        }
        
        // This is a placeholder for getting the actual cursor position.
        // In a real application, you'd use P/Invoke (Win32) to get the cursor position
        // and convert it to screen coordinates.
        private PixelPoint GetCurrentCursorPosition()
        {
            // Placeholder: Returns a fixed position near the center/top of the screen.
            // Replace with platform-specific code to get the typing cursor position.
            return new PixelPoint(1000, 500); 
        }
    }
}