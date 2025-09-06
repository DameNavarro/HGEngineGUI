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
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HGEngineGUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContentFrame.Navigate(typeof(Pages.StartPage));

            // Wire up back navigation for the top NavigationView
            RootNavView.BackRequested += OnBackRequested;
            ContentFrame.Navigated += OnContentNavigated;

            // Try to set a custom window icon if present at Assets\\App.ico
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "App.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    appWindow?.SetIcon(iconPath);
                }
            }
            catch { /* non-fatal if icon cannot be set */ }
            UpdateStatus("Ready");
        }

        private void OnNavigationSelectionChanged(object sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs e)
        {
            if (e.SelectedItem is Microsoft.UI.Xaml.Controls.NavigationViewItem item)
            {
                switch (item.Tag?.ToString())
                {
                    case "Start":
                        ContentFrame.Navigate(typeof(Pages.StartPage));
                        break;
                    case "Species":
                        ContentFrame.Navigate(typeof(Pages.SpeciesListPage));
                        break;
                    case "Trainers":
                        ContentFrame.Navigate(typeof(Pages.TrainersPage));
                        break;
                    case "Items":
                        ContentFrame.Navigate(typeof(Pages.ItemsPage));
                        break;
                    case "Moves":
                        ContentFrame.Navigate(typeof(Pages.MovesPage));
                        break;
                    case "TMHM":
                        ContentFrame.Navigate(typeof(Pages.TMHMPage));
                        break;
                    case "Encounters":
                        ContentFrame.Navigate(typeof(Pages.EncountersPage));
                        break;
                    case "Config":
                        ContentFrame.Navigate(typeof(Pages.ConfigPage));
                        break;
                    // Learnsets/Evolutions are accessible under the Pokémon page; no direct nav needed.
                }
                UpdateStatus($"Navigated: {item.Content}");
            }
        }

        public void UpdateStatus(string message, string timing = "")
        {
            try
            {
                StatusProject.Text = ProjectContext.RootPath ?? "(no project)";
                StatusMessage.Text = message;
                StatusTiming.Text = timing;
            }
            catch { }
        }

        // Public navigation helper so pages (e.g., StartPage) can request navigation
        public void Navigate(Type pageType)
        {
            try
            {
                if (pageType != null)
                {
                    ContentFrame.Navigate(pageType);
                    UpdateStatus($"Navigated: {pageType.Name}");
                }
            }
            catch { }
        }

        private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
            UpdateBackButton();
        }

        private void OnContentNavigated(object sender, NavigationEventArgs e)
        {
            UpdateBackButton();
        }

        private void UpdateBackButton()
        {
            try
            {
                RootNavView.IsBackEnabled = ContentFrame.CanGoBack;
            }
            catch { }
        }
    }
}
