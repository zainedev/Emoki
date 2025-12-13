using Avalonia.Controls;
using System;

namespace Emoki.UI
{
    public partial class PopupView : Window
    {
        public PopupView()
        {
            InitializeComponent();
            DataContext = new PopupViewModel();
        }
    }
}