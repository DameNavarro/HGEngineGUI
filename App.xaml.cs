using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HGEngineGUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            this.UnhandledException += App_UnhandledException;
            try
            {
                AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
                {
                    try
                    {
                        var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                        var path = System.IO.Path.Combine(folder.Path, "first_chance.log");
                        System.IO.File.AppendAllText(path, $"{DateTime.Now:u}\n{e.Exception}\n---\n");
                    }
                    catch { }
                };
            }
            catch { }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            _window.Activate();
            MainWindow = _window;
        }

        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true;
                var message = e.Exception?.Message ?? e.Message;
                // Persist to a simple log for packaged builds
                try
                {
                    var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    var logPath = System.IO.Path.Combine(folder.Path, "last_crash.txt");
                    await System.IO.File.WriteAllTextAsync(logPath, ($"{DateTime.Now:u}\n{message}\n{e.Exception}"));
                }
                catch { }

                if (MainWindow is not null)
                {
                    var dlg = new ContentDialog
                    {
                        Title = "Unexpected error",
                        Content = message,
                        PrimaryButtonText = "Close",
                        XamlRoot = (MainWindow as Window)?.Content.XamlRoot
                    };
                    await dlg.ShowAsync();
                }
            }
            catch { }
        }
    }
}
