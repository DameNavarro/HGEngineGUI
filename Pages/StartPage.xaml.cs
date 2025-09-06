using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGEngineGUI.Pages
{
    public sealed partial class StartPage : Page
    {
        public System.Collections.Generic.IReadOnlyList<HGEngineGUI.Services.ChangeLog.Change> changes => HGEngineGUI.Services.ChangeLog.Changes;
        public StartPage()
        {
            InitializeComponent();
            RefreshUi();
        }

        private void RefreshUi()
        {
            RootPathText.Text = ProjectContext.RootPath ?? "(not selected)";
            SpeciesCountText.Text = ProjectContext.SpeciesCount >= 0 ? $"Species: {ProjectContext.SpeciesCount}" : "Species: -";
        }

        private async void OnPickFolderClick(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                ProjectContext.SetRoot(folder.Path);
                await Data.HGParsers.RefreshCachesAsync();
                RefreshUi();
                // Start watching the armips/data folder for changes
                TryStartWatcher();
            }
        }

        private Services.FileWatcher? _watcher;
        private void TryStartWatcher()
        {
            try
            {
                _watcher?.Dispose();
                if (ProjectContext.RootPath == null) return;
                var dataDir = System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data");
                _watcher = new Services.FileWatcher();
                _watcher.FileChanged += (s, path) => DispatcherQueue.TryEnqueue(async () => await OnExternalFileChanged(path));
                _watcher.Start(dataDir, "*");
            }
            catch { }
        }

        private async Task OnExternalFileChanged(string filePath)
        {
            // Prompt: file changed outside; refresh caches?
            var dlg = new ContentDialog
            {
                Title = "External change detected",
                Content = $"{filePath} was modified. Reload data?",
                PrimaryButtonText = "Reload",
                CloseButtonText = "Ignore",
                XamlRoot = this.XamlRoot
            };
            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                var started = DateTime.Now;
                await Data.HGParsers.RefreshCachesAsync();
                RefreshUi();
                (App.MainWindow as HGEngineGUI.MainWindow)?.UpdateStatus("Reloaded", $"{(DateTime.Now - started).TotalMilliseconds:F0} ms");
            }
        }

        private void OnSpecies(object sender, RoutedEventArgs e)
        {
            (App.MainWindow as HGEngineGUI.MainWindow)?.Navigate(typeof(HGEngineGUI.Pages.SpeciesListPage));
        }

        private void OnItems(object sender, RoutedEventArgs e)
        {
            (App.MainWindow as HGEngineGUI.MainWindow)?.Navigate(typeof(HGEngineGUI.Pages.ItemsPage));
        }

        private void OnMoves(object sender, RoutedEventArgs e)
        {
            (App.MainWindow as HGEngineGUI.MainWindow)?.Navigate(typeof(HGEngineGUI.Pages.MovesPage));
        }

        private void OnEncounters(object sender, RoutedEventArgs e)
        {
            (App.MainWindow as HGEngineGUI.MainWindow)?.Navigate(typeof(HGEngineGUI.Pages.EncountersPage));
        }

        private void OnTrainers(object sender, RoutedEventArgs e)
        {
            (App.MainWindow as HGEngineGUI.MainWindow)?.Navigate(typeof(HGEngineGUI.Pages.TrainersPage));
        }

        private async void OnOpenChangeClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HGEngineGUI.Services.ChangeLog.Change change)
            {
                try
                {
                    var path = change.FilePath;
                    if (System.IO.File.Exists(path))
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri($"file:///{path.Replace("\\", "/")}"));
                    }
                }
                catch { }
            }
        }

        private async void OnRestoreChangeClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HGEngineGUI.Services.ChangeLog.Change change)
            {
                var path = change.FilePath;
                var bak = path + ".bak";
                if (!System.IO.File.Exists(bak))
                {
                    var dlgNoBak = new ContentDialog { Title = "Restore", Content = "No .bak found.", PrimaryButtonText = "OK", XamlRoot = this.XamlRoot };
                    await dlgNoBak.ShowAsync();
                    return;
                }
                var dlg = new ContentDialog
                {
                    Title = "Restore backup",
                    Content = $"Replace current file with backup?\n{path}\n<= {bak}",
                    PrimaryButtonText = "Restore",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };
                var res = await dlg.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    try
                    {
                        var original = await System.IO.File.ReadAllTextAsync(path);
                        var backup = await System.IO.File.ReadAllTextAsync(bak);
                        await System.IO.File.WriteAllTextAsync(path, backup);
                        HGEngineGUI.Services.ChangeLog.Record(path, backup.Length);
                        (App.MainWindow as HGEngineGUI.MainWindow)?.UpdateStatus("Restored from backup");
                    }
                    catch (Exception ex)
                    {
                        var dlgErr = new ContentDialog { Title = "Error", Content = ex.Message, PrimaryButtonText = "OK", XamlRoot = this.XamlRoot };
                        await dlgErr.ShowAsync();
                    }
                }
            }
        }
    }
}


