using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HGEngineGUI.Pages
{
    public sealed partial class EncountersPage : Page
    {
        private List<Data.HGParsers.EncounterArea> _all = new();
        private List<Data.HGParsers.EncounterArea> _filtered = new();
        private Data.HGParsers.EncounterArea? _current;
        private class AreaListItem { public int Id { get; set; } public string Label { get; set; } = string.Empty; public override string ToString() => Label; }

        public EncountersPage()
        {
            InitializeComponent();
            LoadAsync();
        }

        private async void LoadAsync()
        {
            if (ProjectContext.RootPath == null)
            {
                try
                {
                    var dlg = new ContentDialog { Title = "No project selected", Content = "Open the Project tab and pick your HG Engine folder before using Encounters.", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                    await dlg.ShowAsync();
                }
                catch { }
                return;
            }
            await Data.HGParsers.RefreshEncountersAsync();
            _all = Data.HGParsers.EncounterAreas.ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string term = SearchBox?.Text?.Trim() ?? string.Empty;
            _filtered = string.IsNullOrEmpty(term)
                ? _all
                : _all.Where(a => (a.Label?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 || a.Id.ToString().Equals(term, StringComparison.OrdinalIgnoreCase)).ToList();
            var items = _filtered.Select(a => new AreaListItem { Id = a.Id, Label = !string.IsNullOrWhiteSpace(a.Label) ? a.Label.Trim() : $"Area {a.Id}" }).ToList();
            AreaList.ItemsSource = items;
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void OnOpenFile(object sender, RoutedEventArgs e)
        {
            if (ProjectContext.RootPath == null) return;
            var path = System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "encounters.s");
            if (System.IO.File.Exists(path))
            {
                try { await Windows.System.Launcher.LaunchUriAsync(new Uri($"file:///{path.Replace("\\", "/")}")); } catch { }
            }
        }

        private void OnAreaClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not AreaListItem ali) return;
            int id = ali.Id;
            _current = _all.First(a => a.Id == id);
            RenderCurrent();
        }

        private ComboBox MakeSpeciesCombo(string initial)
        {
            var cb = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.SpeciesMacroNames.ToList(), SelectedItem = initial };
            cb.Text = initial;
            return cb;
        }

        private static int[] ParseProbs(string text, int expectedCount, int[] fallback)
        {
            try
            {
                var parts = (text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var vals = parts.Select(p => int.TryParse(p, out var v) ? v : 0).ToArray();
                if (vals.Length == expectedCount) return vals;
            }
            catch { }
            return fallback.ToArray();
        }

        private enum GrassListKind { Morning, Day, Night, Hoenn, Sinnoh }

        private void FillGrassList(ItemsControl ic, GrassListKind kind)
        {
            if (_current == null) return;
            List<string> list = kind switch
            {
                GrassListKind.Morning => _current.MorningGrass,
                GrassListKind.Day => _current.DayGrass,
                GrassListKind.Night => _current.NightGrass,
                GrassListKind.Hoenn => _current.HoennGrass,
                _ => _current.SinnohGrass,
            };
            int[]? useProbs = (kind == GrassListKind.Morning || kind == GrassListKind.Day || kind == GrassListKind.Night) ? _current.GrassProbabilities : null;
            ic.Items.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
                if (useProbs != null && i < useProbs.Length)
                {
                    var prob = new TextBlock { Text = useProbs[i] + "%", VerticalAlignment = VerticalAlignment.Center };
                    row.Children.Add(prob); Grid.SetColumn(prob, 1);
                }
                var img = new Image { Width = 32, Height = 32 };
                var icon = Services.SpriteLocator.FindIconForSpeciesMacroName(list[i]);
                if (icon != null) img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(icon));
                row.Children.Add(img); Grid.SetColumn(img, 0);
                var cb = MakeSpeciesCombo(list[i]);
                int idx = i;
                cb.SelectionChanged += (s, e) => { list[idx] = (cb.SelectedItem as string) ?? (cb.Text ?? string.Empty); };
                cb.LostFocus += (s, e) => { list[idx] = cb.Text ?? string.Empty; };
                row.Children.Add(cb); Grid.SetColumn(cb, 2);
                // Show level from WalkLevels[idx]
                int levelVal = (idx < _current.WalkLevels.Length) ? _current.WalkLevels[idx] : 0;
                var lvl = new TextBox { Width = 64, Text = levelVal.ToString() };
                lvl.TextChanging += (s, e) => { if (int.TryParse(lvl.Text, out var v) && idx < _current.WalkLevels.Length) _current.WalkLevels[idx] = v; };
                row.Children.Add(lvl); Grid.SetColumn(lvl, 3);
                ic.Items.Add(row);
            }
        }

        private StackPanel MakeEncounterRow(Data.HGParsers.EncounterSlot slot, int? probabilityPercent, System.Action moveUp, System.Action moveDown)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            if (probabilityPercent.HasValue)
            {
                sp.Children.Add(new TextBlock { Text = probabilityPercent.Value + "%", Width = 36, VerticalAlignment = VerticalAlignment.Center });
            }
            // sprite
            var img = new Image { Width = 40, Height = 40, Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill };
            var icon = Services.SpriteLocator.FindIconForSpeciesMacroName(slot.SpeciesMacro);
            if (icon != null) img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(icon));
            sp.Children.Add(img);
            // species
            var cb = MakeSpeciesCombo(slot.SpeciesMacro);
            cb.SelectionChanged += (s, e) => slot.SpeciesMacro = (cb.SelectedItem as string) ?? (cb.Text ?? string.Empty);
            cb.LostFocus += (s, e) => slot.SpeciesMacro = cb.Text ?? string.Empty;
            sp.Children.Add(cb);
            // levels
            var minBox = new TextBox { Width = 60, Text = slot.MinLevel.ToString() };
            minBox.TextChanging += (s, e) => { if (int.TryParse(minBox.Text, out var v)) slot.MinLevel = v; };
            var maxBox = new TextBox { Width = 60, Text = slot.MaxLevel.ToString() };
            maxBox.TextChanging += (s, e) => { if (int.TryParse(maxBox.Text, out var v)) slot.MaxLevel = v; };
            sp.Children.Add(new StackPanel { Children = { new TextBlock { Text = "Min" }, minBox } });
            sp.Children.Add(new StackPanel { Children = { new TextBlock { Text = "Max" }, maxBox } });
            // Reorder buttons
            var up = new Button { Content = "↑" }; up.Click += (s, e) => moveUp();
            var down = new Button { Content = "↓" }; down.Click += (s, e) => moveDown();
            sp.Children.Add(up); sp.Children.Add(down);
            return sp;
        }

        private void RenderCurrent()
        {
            if (_current == null) return;
            HeaderText.Text = $"{_current.Id}: {_current.Label}";
            WalkRateBox.Text = _current.WalkRate.ToString();
            SurfRateBox.Text = _current.SurfRate.ToString();
            RockRateBox.Text = _current.RockSmashRate.ToString();
            OldRodRateBox.Text = _current.OldRodRate.ToString();
            GoodRodRateBox.Text = _current.GoodRodRate.ToString();
            SuperRodRateBox.Text = _current.SuperRodRate.ToString();

            MorningProbs.Text = string.Join(", ", _current.GrassProbabilities);
            DayProbs.Text = string.Join(", ", _current.GrassProbabilities);
            NightProbs.Text = string.Join(", ", _current.GrassProbabilities);
            GrassLevelsBox.Text = string.Join(", ", _current.WalkLevels);
            FillGrassList(MorningList, GrassListKind.Morning);
            FillGrassList(DayList, GrassListKind.Day);
            FillGrassList(NightList, GrassListKind.Night);
            FillGrassList(HoennList, GrassListKind.Hoenn);
            FillGrassList(SinnohList, GrassListKind.Sinnoh);

            SurfList.Items.Clear();
            for (int i = 0; i < _current.Surf.Count; i++)
            {
                int idx = i;
                SurfList.Items.Add(MakeEncounterRow(_current.Surf[i], i < _current.SurfProbabilities.Length ? _current.SurfProbabilities[i] : (int?)null,
                    moveUp: () => { if (idx > 0) { (_current.Surf[idx - 1], _current.Surf[idx]) = (_current.Surf[idx], _current.Surf[idx - 1]); RenderCurrent(); } },
                    moveDown: () => { if (idx + 1 < _current.Surf.Count) { (_current.Surf[idx + 1], _current.Surf[idx]) = (_current.Surf[idx], _current.Surf[idx + 1]); RenderCurrent(); } }));
            }
            RockList.Items.Clear();
            for (int i = 0; i < _current.RockSmash.Count; i++)
            {
                int idx = i;
                RockList.Items.Add(MakeEncounterRow(_current.RockSmash[i], i < _current.RockProbabilities.Length ? _current.RockProbabilities[i] : (int?)null,
                    moveUp: () => { if (idx > 0) { (_current.RockSmash[idx - 1], _current.RockSmash[idx]) = (_current.RockSmash[idx], _current.RockSmash[idx - 1]); RenderCurrent(); } },
                    moveDown: () => { if (idx + 1 < _current.RockSmash.Count) { (_current.RockSmash[idx + 1], _current.RockSmash[idx]) = (_current.RockSmash[idx], _current.RockSmash[idx + 1]); RenderCurrent(); } }));
            }
            OldRodList.Items.Clear();
            for (int i = 0; i < _current.OldRod.Count; i++)
            {
                int idx = i;
                OldRodList.Items.Add(MakeEncounterRow(_current.OldRod[i], i < _current.OldRodProbabilities.Length ? _current.OldRodProbabilities[i] : (int?)null,
                    moveUp: () => { if (idx > 0) { (_current.OldRod[idx - 1], _current.OldRod[idx]) = (_current.OldRod[idx], _current.OldRod[idx - 1]); RenderCurrent(); } },
                    moveDown: () => { if (idx + 1 < _current.OldRod.Count) { (_current.OldRod[idx + 1], _current.OldRod[idx]) = (_current.OldRod[idx], _current.OldRod[idx + 1]); RenderCurrent(); } }));
            }
            GoodRodList.Items.Clear();
            for (int i = 0; i < _current.GoodRod.Count; i++)
            {
                int idx = i;
                GoodRodList.Items.Add(MakeEncounterRow(_current.GoodRod[i], i < _current.GoodRodProbabilities.Length ? _current.GoodRodProbabilities[i] : (int?)null,
                    moveUp: () => { if (idx > 0) { (_current.GoodRod[idx - 1], _current.GoodRod[idx]) = (_current.GoodRod[idx], _current.GoodRod[idx - 1]); RenderCurrent(); } },
                    moveDown: () => { if (idx + 1 < _current.GoodRod.Count) { (_current.GoodRod[idx + 1], _current.GoodRod[idx]) = (_current.GoodRod[idx], _current.GoodRod[idx + 1]); RenderCurrent(); } }));
            }
            SuperRodList.Items.Clear();
            for (int i = 0; i < _current.SuperRod.Count; i++)
            {
                int idx = i;
                SuperRodList.Items.Add(MakeEncounterRow(_current.SuperRod[i], i < _current.SuperRodProbabilities.Length ? _current.SuperRodProbabilities[i] : (int?)null,
                    moveUp: () => { if (idx > 0) { (_current.SuperRod[idx - 1], _current.SuperRod[idx]) = (_current.SuperRod[idx], _current.SuperRod[idx - 1]); RenderCurrent(); } },
                    moveDown: () => { if (idx + 1 < _current.SuperRod.Count) { (_current.SuperRod[idx + 1], _current.SuperRod[idx]) = (_current.SuperRod[idx], _current.SuperRod[idx + 1]); RenderCurrent(); } }));
            }

            SwarmGrassCombo.ItemsSource = Data.HGParsers.SpeciesMacroNames; SwarmGrassCombo.Text = _current.SwarmGrass;
            SwarmSurfCombo.ItemsSource = Data.HGParsers.SpeciesMacroNames; SwarmSurfCombo.Text = _current.SwarmSurf;
            SwarmGoodRodCombo.ItemsSource = Data.HGParsers.SpeciesMacroNames; SwarmGoodRodCombo.Text = _current.SwarmGoodRod;
            SwarmSuperRodCombo.ItemsSource = Data.HGParsers.SpeciesMacroNames; SwarmSuperRodCombo.Text = _current.SwarmSuperRod;
        }

        private async void OnPreview(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            // Sync basic rates
            if (int.TryParse(WalkRateBox.Text, out var wr)) _current.WalkRate = wr;
            if (int.TryParse(SurfRateBox.Text, out var sr)) _current.SurfRate = sr;
            if (int.TryParse(RockRateBox.Text, out var rr)) _current.RockSmashRate = rr;
            if (int.TryParse(OldRodRateBox.Text, out var orr)) _current.OldRodRate = orr;
            if (int.TryParse(GoodRodRateBox.Text, out var grr)) _current.GoodRodRate = grr;
            if (int.TryParse(SuperRodRateBox.Text, out var srr)) _current.SuperRodRate = srr;
            _current.SwarmGrass = SwarmGrassCombo.Text ?? string.Empty;
            _current.SwarmSurf = SwarmSurfCombo.Text ?? string.Empty;
            _current.SwarmGoodRod = SwarmGoodRodCombo.Text ?? string.Empty;
            _current.SwarmSuperRod = SwarmSuperRodCombo.Text ?? string.Empty;
            // Parse grass levels box
            try
            {
                var parts = (GrassLevelsBox.Text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 12)
                {
                    for (int i = 0; i < 12; i++) _current.WalkLevels[i] = int.TryParse(parts[i], out var v) ? v : _current.WalkLevels[i];
                }
            }
            catch { }

            _current.GrassProbabilities = ParseProbs(MorningProbs.Text, 12, _current.GrassProbabilities);
            _current.SurfProbabilities = ParseProbs(SurfProbs.Text, _current.Surf.Count, _current.SurfProbabilities);
            _current.RockProbabilities = ParseProbs(RockProbs.Text, _current.RockSmash.Count, _current.RockProbabilities);
            _current.OldRodProbabilities = ParseProbs(OldRodProbs.Text, _current.OldRod.Count, _current.OldRodProbabilities);
            _current.GoodRodProbabilities = ParseProbs(GoodRodProbs.Text, _current.GoodRod.Count, _current.GoodRodProbabilities);
            _current.SuperRodProbabilities = ParseProbs(SuperRodProbs.Text, _current.SuperRod.Count, _current.SuperRodProbabilities);
            var diff = await Data.HGSerializers.PreviewEncounterAreaAsync(_current);
            var dlg = new ContentDialog
            {
                Title = "Preview encounters.s changes",
                PrimaryButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Content = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new TextBox { Text = diff, IsReadOnly = true, TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas") }
                }
            };
            await dlg.ShowAsync();
        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            if (_current == null) return;
            if (int.TryParse(WalkRateBox.Text, out var wr)) _current.WalkRate = wr;
            if (int.TryParse(SurfRateBox.Text, out var sr)) _current.SurfRate = sr;
            if (int.TryParse(RockRateBox.Text, out var rr)) _current.RockSmashRate = rr;
            if (int.TryParse(OldRodRateBox.Text, out var orr)) _current.OldRodRate = orr;
            if (int.TryParse(GoodRodRateBox.Text, out var grr)) _current.GoodRodRate = grr;
            if (int.TryParse(SuperRodRateBox.Text, out var srr)) _current.SuperRodRate = srr;
            _current.SwarmGrass = SwarmGrassCombo.Text ?? string.Empty;
            _current.SwarmSurf = SwarmSurfCombo.Text ?? string.Empty;
            _current.SwarmGoodRod = SwarmGoodRodCombo.Text ?? string.Empty;
            _current.SwarmSuperRod = SwarmSuperRodCombo.Text ?? string.Empty;
            try
            {
                var parts = (GrassLevelsBox.Text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 12)
                {
                    for (int i = 0; i < 12; i++) _current.WalkLevels[i] = int.TryParse(parts[i], out var v) ? v : _current.WalkLevels[i];
                }
            }
            catch { }
            _current.GrassProbabilities = ParseProbs(MorningProbs.Text, 12, _current.GrassProbabilities);
            _current.SurfProbabilities = ParseProbs(SurfProbs.Text, _current.Surf.Count, _current.SurfProbabilities);
            _current.RockProbabilities = ParseProbs(RockProbs.Text, _current.RockSmash.Count, _current.RockProbabilities);
            _current.OldRodProbabilities = ParseProbs(OldRodProbs.Text, _current.OldRod.Count, _current.OldRodProbabilities);
            _current.GoodRodProbabilities = ParseProbs(GoodRodProbs.Text, _current.GoodRod.Count, _current.GoodRodProbabilities);
            _current.SuperRodProbabilities = ParseProbs(SuperRodProbs.Text, _current.SuperRod.Count, _current.SuperRodProbabilities);
            await Data.HGSerializers.SaveEncounterAreaAsync(_current);
            var dlg = new ContentDialog { Title = "Saved", Content = "encounters.s updated.", PrimaryButtonText = "OK", XamlRoot = this.XamlRoot };
            await dlg.ShowAsync();
        }

        private async void OnRefresh(object sender, RoutedEventArgs e)
        {
            await Data.HGParsers.RefreshEncountersAsync();
            _all = Data.HGParsers.EncounterAreas.ToList();
            ApplyFilter();
            if (_current != null)
            {
                var match = _all.FirstOrDefault(a => a.Id == _current.Id);
                if (match != null)
                {
                    _current = match;
                    RenderCurrent();
                }
            }
        }
    }
}


