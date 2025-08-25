using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HGEngineGUI.Data;

namespace HGEngineGUI.Pages
{
    public sealed partial class ItemsPage : Page
    {
        private List<(string ItemMacro, int Price)> _items = new();
        private (string ItemMacro, int Price)? _selected;
        private List<HGEngineGUI.Data.HGParsers.MartSection> _marts = new();
        private HGEngineGUI.Data.HGParsers.MartSection? _selectedMart;

        public ItemsPage()
        {
            this.InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await HGEngineGUI.Data.HGParsers.RefreshCachesAsync();
            _items = HGEngineGUI.Data.HGParsers.ItemsWithPrices.ToList();
            ItemList.ItemsSource = _items.Select(t => t.ItemMacro).ToList();

            if (HGEngineGUI.Data.HGParsers.HasMartItems)
            {
                MartExpander.IsEnabled = true;
                MartStatus.Text = "mart_items.s detected";
                _marts = HGEngineGUI.Data.HGParsers.MartSections.ToList();
                MartSectionCombo.ItemsSource = _marts.Select(m => m.Label).ToList();
            }
            else
            {
                MartExpander.IsEnabled = false; // greys out per request
                MartStatus.Text = "mart_items.s not found in armips/asm/custom";
            }
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text?.Trim() ?? string.Empty;
            var src = string.IsNullOrWhiteSpace(q) ? _items : _items.Where(it => it.ItemMacro.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            ItemList.ItemsSource = src.Select(t => t.ItemMacro).ToList();
        }

        private void OnItemClick(object sender, ItemClickEventArgs e)
        {
            var macro = e.ClickedItem?.ToString() ?? string.Empty;
            _selected = _items.FirstOrDefault(t => t.ItemMacro == macro);
            SelectedItemText.Text = macro;
            PriceBox.Text = _selected?.Price.ToString() ?? "";
        }

        private async void OnPreviewPrices(object sender, RoutedEventArgs e)
        {
            if (_selected is null) return;
            if (!int.TryParse(PriceBox.Text.Trim(), out var p)) return;
            var entries = new List<(string ItemMacro, int Price)> { (_selected.Value.ItemMacro, p) };
            var diff = await HGEngineGUI.Data.HGSerializers.PreviewItemPricesAsync(entries);
            await ShowDiffAsync(diff);
        }

        private async void OnSavePrices(object sender, RoutedEventArgs e)
        {
            if (_selected is null) return;
            if (!int.TryParse(PriceBox.Text.Trim(), out var p)) return;
            var entries = new List<(string ItemMacro, int Price)> { (_selected.Value.ItemMacro, p) };
            await HGEngineGUI.Data.HGSerializers.SaveItemPricesAsync(entries);
            await InitializeAsync();
        }

        private void OnMartSectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var idx = MartSectionCombo.SelectedIndex;
            if (idx < 0 || idx >= _marts.Count) { _selectedMart = null; MartItemsControl.ItemsSource = null; return; }
            _selectedMart = _marts[idx];
            RefreshMartItemsUI();
        }

        private void RefreshMartItemsUI()
        {
            if (_selectedMart == null)
            {
                MartItemsControl.ItemsSource = null; return;
            }
            var list = new List<StackPanel>();
            foreach (var it in _selectedMart.Items)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var itemBox = new ComboBox { IsEditable = true, Width = 300 };
                itemBox.ItemsSource = HGEngineGUI.Data.HGParsers.ItemMacros;
                itemBox.Text = it.Item;
                sp.Children.Add(itemBox);
                if (_selectedMart.IsGeneralTable)
                {
                    var badgeBox = new ComboBox { IsEditable = true, Width = 200 };
                    badgeBox.ItemsSource = new[] { "ZERO_BADGES", "ONE_BADGE", "TWO_BADGES", "THREE_BADGES", "FOUR_BADGES", "FIVE_BADGES", "SIX_BADGES", "SEVEN_BADGES", "EIGHT_BADGES" };
                    badgeBox.Text = it.BadgeMacro ?? "ZERO_BADGES";
                    sp.Children.Add(badgeBox);
                }
                list.Add(sp);
            }
            MartItemsControl.ItemsSource = list;
        }

        private async void OnPreviewMarts(object sender, RoutedEventArgs e)
        {
            if (_selectedMart == null) return;
            CaptureMartItemsFromUI();
            var diff = await HGEngineGUI.Data.HGSerializers.PreviewMartItemsAsync(new List<HGEngineGUI.Data.HGParsers.MartSection> { _selectedMart });
            await ShowDiffAsync(diff);
        }

        private async void OnSaveMarts(object sender, RoutedEventArgs e)
        {
            if (_selectedMart == null) return;
            CaptureMartItemsFromUI();
            // Force refresh of latest file to align with saving order
            await HGEngineGUI.Data.HGSerializers.SaveMartItemsAsync(new List<HGEngineGUI.Data.HGParsers.MartSection> { _selectedMart });
            await InitializeAsync();
        }

        private void CaptureMartItemsFromUI()
        {
            if (_selectedMart == null) return;
            var idx = MartSectionCombo.SelectedIndex;
            if (idx < 0) return;
            var newItems = new List<HGEngineGUI.Data.HGParsers.MartItemEntry>();
            foreach (var obj in MartItemsControl.Items)
            {
                if (obj is StackPanel sp && sp.Children.Count > 0)
                {
                    var itemBox = sp.Children[0] as ComboBox;
                    string item = itemBox?.Text?.Trim() ?? string.Empty;
                    string? badge = null;
                    if (_marts[idx].IsGeneralTable && sp.Children.Count > 1)
                    {
                        var badgeBox = sp.Children[1] as ComboBox;
                        badge = badgeBox?.Text?.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(item)) newItems.Add(new HGEngineGUI.Data.HGParsers.MartItemEntry { Item = item, BadgeMacro = badge });
                }
            }
            // Update both the backing collection and the currently selected reference
            _marts[idx].Items = newItems;
            _selectedMart = _marts[idx];
        }

        private async Task ShowDiffAsync(string diff)
        {
            if (string.IsNullOrWhiteSpace(diff)) return;
            ContentDialog dlg = new ContentDialog
            {
                Title = "Preview Changes",
                Content = new ScrollViewer { Content = new TextBlock { Text = diff, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"), TextWrapping = TextWrapping.NoWrap } },
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };
            await dlg.ShowAsync();
        }
    }
}


