using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Emoki
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // We run without a main window, as the popup is the primary visible component.
                // desktop.MainWindow = new MainWindow(); 
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}