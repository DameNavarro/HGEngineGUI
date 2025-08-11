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
    }
}
