using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emoki.UI
{
    // Lightweight result record exposed to the view model and UI
    public record PopupResult(string Shortcut, string Emoji);

    // PopupService: manages creation, positioning and visibility of the suggestion popup.
    public class PopupService
    {
        // Backing window instance for suggestions (kept hidden until needed)
        private PopupView? _popupWindow;

        // Event invoked when a suggestion is explicitly selected (mouse click)
        public static Action<PopupResult>? OnEmojiSelected;

        // Cached active selection stored on the UI thread and read from other threads.
        private static PopupResult? _cachedActiveSelection;

        // Update the cached active selection (called from UI thread)
        public static void UpdateCachedSelection(PopupResult? sel)
        {
            _cachedActiveSelection = sel;
        }

        // Public accessor returning the current hovered/selected item, if any.
        // Returns a cached copy safe to read from non-UI threads.
        public PopupResult? GetActiveSelection()
        {
            return _cachedActiveSelection;
        }

        // Create the hidden popup window and capture its platform handle for hook logic.
        // This is called after Avalonia platform initialization to avoid early-window errors.
        public void InitializeWindowHandle()
        {
            if (_popupWindow == null)
            {
                _popupWindow = new PopupView();
                _popupWindow.CanResize = false;
                _popupWindow.SizeToContent = SizeToContent.WidthAndHeight;
                _popupWindow.IsVisible = false;

                // Temporarily show to ensure a platform handle is created
                _popupWindow.Show();

                var platformHandle = _popupWindow.TryGetPlatformHandle();
                if (platformHandle != null)
                {
                    Emoki.Platforms.Windows.KeyboardHook.PopupHandle = platformHandle.Handle;
                }

                // Hide again on the UI thread to keep the window invisible until needed
                Dispatcher.UIThread.Post(() =>
                {
                    if (_popupWindow != null)
                        _popupWindow.IsVisible = false;
                });
            }
        }

        // Show the popup with `searchResults` (shortcut -> emoji). Ensures ViewModel updates.
        public void ShowPopup(List<KeyValuePair<string, string>> searchResults)
        {
            if (searchResults == null || !searchResults.Any())
            {
                HidePopup();
                return;
            }

            if (_popupWindow == null) InitializeWindowHandle();

            // Defensive: ensure DataContext is the expected view model before casting
            if (!(_popupWindow!.DataContext is PopupViewModel viewModel))
                return;

            var popupResults = searchResults.Select(kvp => new PopupResult(kvp.Key, kvp.Value)).ToList();
            viewModel.UpdateResults(popupResults);

            // Set cached active selection to the first result by default
            if (popupResults.Count > 0)
                UpdateCachedSelection(popupResults[0]);

            var screenPosition = GetCurrentCursorPosition();
            _popupWindow.Position = new PixelPoint((int)screenPosition.X, (int)screenPosition.Y);

            if (!_popupWindow.IsVisible)
            {
                _popupWindow.Show();
            }
        }

        // Hide the popup if present
        public void HidePopup()
        {
            if (_popupWindow != null)
            {
                _popupWindow.IsVisible = false;
                // Clear cached selection so Enter is not suppressed after hiding
                UpdateCachedSelection(null);
            }
        }

        // Move the view-model selection up one item (UI thread)
        public void MoveSelectionUp()
        {
            if (_popupWindow == null) return;
            if (!(_popupWindow.DataContext is PopupViewModel vm)) return;
            if (vm.Results == null || vm.Results.Count == 0) return;

            if (vm.SelectedIndex > 0)
                vm.SelectedIndex -= 1;

            UpdateCachedSelection(vm.SelectedResult);
        }

        // Move the view-model selection down one item (UI thread)
        public void MoveSelectionDown()
        {
            if (_popupWindow == null) return;
            if (!(_popupWindow.DataContext is PopupViewModel vm)) return;
            if (vm.Results == null || vm.Results.Count == 0) return;

            if (vm.SelectedIndex < vm.Results.Count - 1)
                vm.SelectedIndex += 1;

            UpdateCachedSelection(vm.SelectedResult);
        }

        // Placeholder: compute appropriate cursor-based placement for the popup.
        // Currently returns a fixed point; replace with real cursor query when available.
        private PixelPoint GetCurrentCursorPosition()
        {
            return new PixelPoint(1100, 450);
        }
    }
}