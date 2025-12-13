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
            
            // --- Fixes Focus Stealing ---
            this.ShowActivated = false;
            
            // --- Fixes Z-Order (Always On Top) ---
            this.Topmost = true; 
            
            // NOTE: Do not call this.Show() here. It belongs in the service class.
            // When a window is created within a service/VM, it should be shown by the
            // logic that manages it (PopupService), not in its own constructor.
            // If the application is working now, keep the PopupService logic as is.

            DataContext = new PopupViewModel();
        }
    }
}