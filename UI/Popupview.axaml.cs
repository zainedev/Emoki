using Avalonia.Controls;
using System;

namespace Emoki.UI
{
    public partial class PopupView : Window
    {
        public PopupView()
        {
            InitializeComponent();
            this.ShowActivated = false;
            this.Topmost = true;
            
            var listBox = this.FindControl<ListBox>("EmojiList");
            if (listBox != null)
            {
                listBox.SelectionChanged += (s, e) =>
                {
                    if (listBox.SelectedItem is PopupResult selected)
                    {
                        // Trigger the global selection event
                        PopupService.OnEmojiSelected?.Invoke(selected);
                        
                        // Clear selection immediately so clicking the same one again works later
                        listBox.SelectedItem = null;
                    }
                };
            }

            DataContext = new PopupViewModel();
        }
    }
}