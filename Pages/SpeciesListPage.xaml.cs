using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HGEngineGUI.Pages
{
    public sealed partial class SpeciesListPage : Page
    {
        private List<SpeciesEntry> _all = new();
        private List<SpeciesEntry> _filtered = new();

        public SpeciesListPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            _all = Data.HGParsers.Species
                .Select(s => s with { IconPath = Services.SpriteLocator.FindIconForSpeciesMacroName(s.Name) ?? string.Empty })
                .ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var term = SearchBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(term))
                _filtered = _all;
            else
                _filtered = _all.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || s.Id.ToString().Contains(term)).ToList();
            SpeciesList.ItemsSource = _filtered;
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
    }
}


