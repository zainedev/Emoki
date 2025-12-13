using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Emoki.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input; // Note: Avalonia uses its own Input system, but this is a placeholder

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
            // This requires calling into Win32 or using platform-specific Avalonia methods.
            // For simplicity in this demo, we'll use a placeholder position near the center/top.
            var screenPosition = GetCurrentCursorPosition();
            
            if (_popupWindow == null)
            {
                _popupWindow = new PopupView();
                _popupWindow.CanResize = false;
                _popupWindow.SizeToContent = Avalonia.Controls.SizeToContent.WidthAndHeight;
                _popupWindow.IsVisible = false;
                
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // If you wanted a true Window, you'd show it here.
                    // For a popup, setting position and showing is enough.
                }
            }
            
            // Set the ViewModel data
            var viewModel = (PopupViewModel)_popupWindow.DataContext!;
            viewModel.UpdateResults(searchResults.Select(kvp => new PopupResult(kvp.Key, kvp.Value)).ToList());
            
            // Position and show
            _popupWindow.Position = new PixelPoint((int)screenPosition.X, (int)screenPosition.Y);
            _popupWindow.IsVisible = true;
            _popupWindow.Activate();
        }

        public void HidePopup()
        {
            if (_popupWindow != null)
            {
                _popupWindow.IsVisible = false;
                // Optional: Dispose or close the window if needed, but hiding is sufficient for quick show/hide.
            }
        }
        
        // This is a placeholder for getting the actual cursor position.
        // In a real application, you'd use P/Invoke (Win32) to get the cursor position
        // and convert it to screen coordinates.
        private Point GetCurrentCursorPosition()
        {
            // Placeholder: Returns a fixed position near the center of the screen
            // In a real app, this should match where the user is typing.
            return new Point(1000, 500); 
        }
    }
}