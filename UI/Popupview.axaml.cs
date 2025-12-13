// UI/PopupView.axaml.cs
using Avalonia.Controls;
using System;

namespace Emoki.UI
{
    public partial class PopupView : Window
    {
        public PopupView()
        {
            InitializeComponent();
            
            // FIXES: Focus and Z-Order
            this.ShowActivated = false; // Prevents the window from stealing focus
            this.Topmost = true;        // Keeps the window above other applications
            this.IsVisible = false;     // Ensure the initial state is hidden
            
            DataContext = new PopupViewModel();
        }
    }
}