using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System;

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
                // CRITICAL FIX 1: Set the application to only close when we explicitly call Shutdown().
                // This prevents the app from quitting when the last "window" (TrayIcon menu) closes.
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown; 

                // Initialize the popup service now that Avalonia's platform is available.
                // This creates the hidden window handle needed for TrayIcon stability.
                try
                {
                    Program.InitializePopupService();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PopupService init failed] {ex}");
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        // The handler for the TrayIcon's Exit button, defined in App.axaml
        private void ExitAppMenuItem_Click(object? sender, System.EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                lifetime.Shutdown();
            }
        }
    }
}