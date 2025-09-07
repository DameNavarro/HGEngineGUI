using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace HGEngineGUI.Pages
{
    public sealed partial class SpeciesListPage : Page
    {
        private List<SpeciesEntry> _all = new();
        private List<SpeciesEntry> _filtered = new();

        public SpeciesListPage()
        {
            InitializeComponent();
            _ = LoadDataAsync();
        }

        private async void OnPageLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (ProjectContext.RootPath != null)
                await Data.HGParsers.RefreshCachesAsync();
            _all = Data.HGParsers.Species
                .Select(s =>
                {
                    string icon = string.Empty;
                    try { icon = Services.SpriteLocator.FindIconForSpeciesMacroName(s.Name) ?? string.Empty; }
                    catch { icon = string.Empty; }
                    return s with { IconPath = icon };
                })
                .ToList();
            ApplyFilter();
            try { StatusText.Text = $"Loaded {_all.Count} species"; } catch { }
        }

        private void ApplyFilter()
        {
            var term = SearchBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(term))
                _filtered = _all.ToList();
            else
                _filtered = _all.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || s.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) || s.Id.ToString().Contains(term)).ToList();
            SpeciesList.ItemsSource = null;
            SpeciesList.ItemsSource = _filtered;
        }

        // Defer icon image creation to container realization to avoid allocating hundreds of BitmapImages at once
        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            try
            {
                if (args.ItemContainer == null) return;
                if (args.Item is not SpeciesEntry entry) return;
                var root = (args.ItemContainer.ContentTemplateRoot as Microsoft.UI.Xaml.FrameworkElement);
                if (root == null) return;
                var image = root.FindName("IconImage") as Microsoft.UI.Xaml.Controls.Image;
                if (image == null) return;
                // Clear first; then set if valid
                image.Source = null;
                var icon = entry.IconPath;
                if (!string.IsNullOrWhiteSpace(icon))
                {
                    try
                    {
                        var uri = new Uri(icon.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ? icon : $"file:///{icon.Replace("\\", "/")}");
                        image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
                    }
                    catch { image.Source = null; }
                }
            }
            catch { }
        }

        private void OnSearchChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is SpeciesEntry entry)
            {
                Frame.Navigate(typeof(SpeciesDetailPage), entry);
            }
        }
    }

    public record SpeciesEntry(int Id, string Name)
    {
        public string IconPath { get; init; } = string.Empty;
        public string DisplayName
        {
            get
            {
                try
                {
                    var s = Name ?? string.Empty;
                    const string prefix = "SPECIES_";
                    if (s.StartsWith(prefix, StringComparison.Ordinal)) s = s.Substring(prefix.Length);
                    s = s.Replace('_', ' ');
                    return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
                }
                catch { return Name; }
            }
        }
    }
}


