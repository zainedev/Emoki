using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Emoki.UI
{
    public partial class PopupView : Window
    {
        public PopupView()
        {
            InitializeComponent();
            ShowActivated = false;
            Topmost = true;
            
            DataContext = new PopupViewModel();

            // Improve hover responsiveness by hit-testing containers on pointer move
            var listBox = this.FindControl<ListBox>("EmojiList");
            if (listBox != null)
            {
                // Instead of the loop, use the event that naturally knows which item was hovered
                listBox.AddHandler(PointerEnteredEvent, (s, e) =>
                {
                    if (e.Source is ListBoxItem item && DataContext is PopupViewModel vm)
                    {
                        vm.SelectedIndex = listBox.IndexFromContainer(item);
                        PopupService.UpdateCachedSelection(vm.Results[vm.SelectedIndex]);
                    }
                }, RoutingStrategies.Bubble);
            }
        }

        // Returns the currently highlighted result from the view model (if any)
        public PopupResult? GetCurrentSelection()
        {
            return (DataContext as PopupViewModel)?.SelectedResult;
        }

        // Pointer enter on an item: update view-model selected index to follow hover
        private void OnItemPointerEnter(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (sender is Avalonia.Controls.Control ctrl && DataContext is PopupViewModel vm && ctrl.DataContext is PopupResult pr)
            {
                int idx = vm.Results.IndexOf(pr);
                if (idx >= 0)
                {
                    vm.SelectedIndex = idx;
                    PopupService.UpdateCachedSelection(pr);
                }
            }
        }

        // Pointer pressed on an item: confirm the current view-model selection
        private void OnItemPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            // Hide popup immediately to return focus to previous window,
            // then send the clicked item for injection.
            this.IsVisible = false;
            if (sender is Avalonia.Controls.Control ctrl && ctrl.DataContext is PopupResult pr)
            {
                PopupService.OnEmojiSelected?.Invoke(pr);
            }
            if (e != null) e.Handled = true;
        }
    }
}