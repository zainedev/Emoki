using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System;

namespace Emoki
{
    // Application-level lifecycle hooks and small helpers
    public partial class App : Application
    {
        // Load XAML resources and App-level styles
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Called when the Avalonia framework is ready. This is executed on the UI thread.
        // We set a conservative ShutdownMode and initialize UI services that require the
        // platform (like creating a hidden popup window for the tray icon).
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Prevent automatic shutdown when windows close; we control shutdown explicitly.
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Initialize the popup service now that Avalonia's platform is available.
                try
                {
                    Program.InitializePopupService();
                }
                catch (Exception ex)
                {
                    // Defensive logging to avoid crashing the app
                    Console.WriteLine($"[PopupService init failed] {ex}");
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        // Handler wired from App.axaml for the tray menu exit action.
        // Calls the desktop lifetime Shutdown so the application exits cleanly.
        private void ExitAppMenuItem_Click(object? sender, System.EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }
    }
}