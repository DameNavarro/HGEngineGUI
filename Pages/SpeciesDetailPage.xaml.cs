using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;

namespace HGEngineGUI.Pages
{
    public sealed partial class SpeciesDetailPage : Page
    {
        private SpeciesEntry? _species;
        private List<(int level, string move)> _levelUp = new();
        private List<LevelUpEntry> _levelUpModel = new();
        private List<string> _egg = new();
        private List<EggMoveEntry> _eggModel = new();
        private List<string> _tmhmAll = new();
        private HashSet<string> _tmhmSelected = new();
        private List<string> _tmhmView = new();
        private List<(string method, int param, string target)> _evolutions = new();
        private List<EvolutionEntry> _evoModel = new();
        private HashSet<string> _tutorSelected = new();
        private List<(string tutor, string move, int cost)> _tutorAll = new();
        private List<(string tutor, string move, int cost)> _tutorView = new();
        private List<string> _tutorViewDisplay = new();

        public SpeciesDetailPage()
        {
            InitializeComponent();
        }

        // Ensure initial move text renders even if SelectedItem isn't resolved yet
        private void OnMoveComboLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb)
                {
                    if (cb.DataContext is LevelUpEntry lu && !string.IsNullOrWhiteSpace(lu.Move))
                    {
                        if (cb.SelectedItem == null && cb.ItemsSource is IEnumerable<string> moves && moves.Contains(lu.Move))
                        {
                            cb.SelectedValue = lu.Move;
                        }
                        cb.Text = lu.Move;
                    }
                    else if (cb.DataContext is EggMoveEntry em && !string.IsNullOrWhiteSpace(em.Move))
                    {
                        if (cb.SelectedItem == null && cb.ItemsSource is IEnumerable<string> moves2 && moves2.Contains(em.Move))
                        {
                            cb.SelectedValue = em.Move;
                        }
                        cb.Text = em.Move;
                    }
                }
            }
            catch
            {
                // Avoid crashing UI if template load ordering causes issues
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _species = e.Parameter as SpeciesEntry;
            if (_species == null) return;

            Title.Text = _species.Name;
            Subtitle.Text = $"ID: {_species.Id}";
            try
            {
                if (!string.IsNullOrWhiteSpace(_species.IconPath))
                {
                    Icon.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(_species.IconPath));
                }
            }
            catch { /* ignore icon load issues */ }

            await Data.HGParsers.RefreshSpeciesDetailsAsync(_species.Id, _species.Name);
            _levelUp = Data.HGParsers.LevelUpMoves.ToList();
            _levelUpModel = _levelUp.Select(m => new LevelUpEntry { Level = m.level, Move = m.move }).ToList();
            LevelUpList.ItemsSource = _levelUpModel;
            _evolutions = Data.HGParsers.Evolutions.ToList();
            _evoModel = _evolutions.Select(e => new EvolutionEntry { Method = e.method, Param = e.param, Target = e.target }).ToList();
            EvolutionList.ItemsSource = _evoModel;
            _egg = Data.HGParsers.EggMoves.ToList();
            _eggModel = _egg.Select(m => new EggMoveEntry { Move = m }).ToList();
            EggMovesList.ItemsSource = _eggModel;
            // Tutor preselection
            _tutorSelected = new HashSet<string>(Data.HGParsers.TutorSelectedForSpecies);
            _tutorAll = Data.HGParsers.TutorHeaders.ToList();
            _tutorView = _tutorAll.ToList();
            _tutorViewDisplay = _tutorView.Select(t => $"{t.tutor}: {t.move} {t.cost}").ToList();
            TutorList.ItemsSource = _tutorViewDisplay;
            TutorList.SelectedItems.Clear();
            foreach (var header in _tutorView)
            {
                var key = $"{header.tutor}: {header.move} {header.cost}";
                if (_tutorSelected.Contains(key)) TutorList.SelectedItems.Add(key);
            }

            // TM/HM list: show all TM labels; then filter via search if needed
            _tmhmAll = Data.HGParsers.TmHmMoves.ToList();
            _tmhmView = _tmhmAll.ToList();
            TmHmList.ItemsSource = _tmhmView;
            // Preselect those that contain this species
            var selected = Data.HGParsers.TmHmSelectedForSpecies;
            TmHmList.SelectedItems.Clear();
            foreach (var hdr in _tmhmView)
            {
                if (selected.Contains(hdr)) TmHmList.SelectedItems.Add(hdr);
            }

            var ov = Data.HGParsers.Overview;
            if (ov != null)
            {
                Type1Box.SelectedItem = ov.Type1;
                Type2Box.SelectedItem = ov.Type2;
                HpBox.Text = ov.BaseHp.ToString();
                AtkBox.Text = ov.BaseAttack.ToString();
                DefBox.Text = ov.BaseDefense.ToString();
                SpABox.Text = ov.BaseSpAttack.ToString();
                SpDBox.Text = ov.BaseSpDefense.ToString();
                SpeBox.Text = ov.BaseSpeed.ToString();
                EvHpBox.Text = ov.EvYields.hp.ToString();
                EvAtkBox.Text = ov.EvYields.atk.ToString();
                EvDefBox.Text = ov.EvYields.def.ToString();
                EvSpABox.Text = ov.EvYields.spatk.ToString();
                EvSpDBox.Text = ov.EvYields.spdef.ToString();
                EvSpeBox.Text = ov.EvYields.spd.ToString();
                Ability1Box.SelectedItem = ov.Ability1;
                Ability2Box.SelectedItem = ov.Ability2;
                Egg1Box.SelectedItem = ov.EggGroup1;
                Egg2Box.SelectedItem = ov.EggGroup2;
                GrowthBox.SelectedItem = ov.GrowthRate;
                GenderBox.Text = ov.GenderRatio.ToString();
                CatchBox.Text = ov.CatchRate.ToString();
                EggCyclesBox.Text = ov.EggCycles.ToString();
                FriendBox.Text = ov.BaseFriendship.ToString();
                Item1Box.SelectedItem = ov.Item1;
                Item2Box.SelectedItem = ov.Item2;
            }
        }

        // Level-up editing (simple; move names must be exact macros)
        private async void OnAddLevelUp(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var movePicker = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.MoveMacros, PlaceholderText = "MOVE_*" };
            var levelBox = new TextBox { Header = "Level" };
            var dialog = new ContentDialog
            {
                Title = "Add Level-up Move",
                Content = new StackPanel { Spacing = 8, Children = { movePicker, levelBox } },
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var move = (movePicker.Text ?? string.Empty).Trim();
                var levelStr = (levelBox.Text ?? string.Empty).Trim();
                if (int.TryParse(levelStr, out var lvl) && !string.IsNullOrEmpty(move))
                {
                    _levelUpModel.Add(new LevelUpEntry { Level = lvl, Move = move });
                    _levelUpModel = _levelUpModel.OrderBy(x => x.Level).ToList();
                    LevelUpList.ItemsSource = null; LevelUpList.ItemsSource = _levelUpModel;
                }
            }
        }

        private void OnDeleteLevelUp(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (LevelUpList.SelectedIndex >= 0)
            {
                _levelUpModel.RemoveAt(LevelUpList.SelectedIndex);
                LevelUpList.ItemsSource = null; LevelUpList.ItemsSource = _levelUpModel;
            }
        }

        private async void OnSaveLevelUp(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            var entries = _levelUpModel.Select(m => (m.Level, m.Move)).OrderBy(m => m.Level).ToList();
            var errors = ValidateLevelUp(entries);
            if (errors.Count > 0)
            {
                await ShowDiffDialog(string.Join("\n", errors), "Fix validation errors");
                return;
            }
            await Data.HGSerializers.SaveLevelUpAsync(_species.Name, entries);
        }

        private async void OnPreviewLevelUp(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var entries = _levelUpModel.Select(m => (m.Level, m.Move)).OrderBy(m => m.Level).ToList();
            var diff = await Data.HGSerializers.PreviewLevelUpAsync(_species.Name, entries);
            await ShowDiffDialog(diff, "levelupdata.s");
        }

        // Egg moves
        private async void OnAddEggMove(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var eggPicker = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.MoveMacros, PlaceholderText = "MOVE_*" };
            var dialog = new ContentDialog
            {
                Title = "Add Egg Move",
                Content = eggPicker,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var text = (eggPicker.Text ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _eggModel.Add(new EggMoveEntry { Move = text });
                    EggMovesList.ItemsSource = null; EggMovesList.ItemsSource = _eggModel;
                }
            }
        }

        private void OnDeleteEggMove(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (EggMovesList.SelectedIndex >= 0)
            {
                _eggModel.RemoveAt(EggMovesList.SelectedIndex);
                EggMovesList.ItemsSource = null; EggMovesList.ItemsSource = _eggModel;
            }
        }

        private async void OnSaveEggMoves(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            var moves = _eggModel.Select(m => m.Move).ToList();
            var errors = ValidateEgg(moves);
            if (errors.Count > 0)
            {
                await ShowDiffDialog(string.Join("\n", errors), "Fix validation errors");
                return;
            }
            await Data.HGSerializers.SaveEggMovesAsync(_species.Name, moves);
        }

        private async void OnPreviewEggMoves(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var diff = await Data.HGSerializers.PreviewEggMovesAsync(_species.Name, _eggModel.Select(m=>m.Move).ToList());
            await ShowDiffDialog(diff, "eggmoves.s");
        }
        private async void OnAddEvolution(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var methodPicker = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.EvolutionMethodMacros, Header = "Method (EVO_*)", MinWidth = 280 };
            var paramBox = new TextBox { Header = "Param (number)" };
            var targetPicker = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.SpeciesMacroNames, Header = "Target (SPECIES_*)", MinWidth = 280 };
            var itemPicker = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.ItemMacros, Header = "Item (EVO_ITEM)", MinWidth = 280 };
            var movePicker = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.MoveMacros, Header = "Move (EVO_MOVE)", MinWidth = 280 };
            var mapPicker = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.MapMacros, Header = "Map (EVO_MAP)", MinWidth = 280 };

            var dialog = new ContentDialog
            {
                Title = "Add Evolution",
                Content = new StackPanel { Spacing = 8, Children = { methodPicker, paramBox, targetPicker, itemPicker, movePicker, mapPicker } },
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };
            var res = await dialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                var method = (methodPicker.SelectedItem as string) ?? (methodPicker.Text ?? string.Empty);
                var target = (targetPicker.SelectedItem as string) ?? (targetPicker.Text ?? string.Empty);
                var paramText = (paramBox.Text ?? string.Empty).Trim();
                // Helper pickers: write numeric param when applicable if user selected an item/move/map
                if (string.Equals(method, "EVO_ITEM", StringComparison.OrdinalIgnoreCase))
                {
                    var macro = (itemPicker.SelectedItem as string) ?? (itemPicker.Text ?? string.Empty);
                    if (Data.HGParsers.TryGetItemValue(macro, out var val)) paramText = val.ToString();
                }
                else if (string.Equals(method, "EVO_MOVE", StringComparison.OrdinalIgnoreCase))
                {
                    var macro = (movePicker.SelectedItem as string) ?? (movePicker.Text ?? string.Empty);
                    if (Data.HGParsers.TryGetMoveValue(macro, out var val)) paramText = val.ToString();
                }
                else if (string.Equals(method, "EVO_MAP", StringComparison.OrdinalIgnoreCase))
                {
                    var macro = (mapPicker.SelectedItem as string) ?? (mapPicker.Text ?? string.Empty);
                    if (Data.HGParsers.TryGetMapValue(macro, out var val)) paramText = val.ToString();
                }
                if (int.TryParse(paramText, out var param) && !string.IsNullOrWhiteSpace(method) && !string.IsNullOrWhiteSpace(target))
                {
                    _evoModel.Add(new EvolutionEntry { Method = method.Trim(), Param = param, Target = target.Trim() });
                    EvolutionList.ItemsSource = null; EvolutionList.ItemsSource = _evoModel;
                }
            }
        }

        private void OnDeleteEvolution(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (EvolutionList.SelectedIndex >= 0)
            {
                _evoModel.RemoveAt(EvolutionList.SelectedIndex);
                EvolutionList.ItemsSource = null; EvolutionList.ItemsSource = _evoModel;
            }
        }

        private async void OnSaveEvolutions(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            var current = _evoModel.Select(e => (e.Method, e.Param, e.Target)).ToList();
            var errors = ValidateEvolutions(current);
            if (errors.Count > 0)
            {
                await ShowDiffDialog(string.Join("\n", errors), "Fix validation errors");
                return;
            }
            await Data.HGSerializers.SaveEvolutionsAsync(_species.Name, current);
        }

        private async void OnPreviewEvolutions(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var diff = await Data.HGSerializers.PreviewEvolutionsAsync(_species.Name, _evoModel.Select(e => (e.Method, e.Param, e.Target)).ToList());
            await ShowDiffDialog(diff, "evodata.s");
        }

        private async void OnSaveTmHm(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            var selected = TmHmList.SelectedItems.Cast<string>().ToList();
            await Data.HGSerializers.SaveTmHmForSpeciesAsync(_species.Name, selected);
        }

        private async void OnPreviewTmHm(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var selected = TmHmList.SelectedItems.Cast<string>().ToList();
            var diff = await Data.HGSerializers.PreviewTmHmAsync(_species.Name, selected);
            await ShowDiffDialog(diff, "tmlearnset.txt");
        }

        private async void OnSaveTutors(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            var selected = TutorList.SelectedItems
                .Cast<string>()
                .Select(s =>
                {
                    // format is "TUTOR_X: MOVE_Y cost"
                    var parts = s.Split(':');
                    var tutor = parts[0].Trim();
                    var rest = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    var moveAndCost = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var move = moveAndCost.Length > 0 ? moveAndCost[0].Trim() : string.Empty;
                    var cost = 0;
                    if (moveAndCost.Length > 1) int.TryParse(moveAndCost[^1], out cost);
                    return (tutor, move, cost);
                })
                .ToList();
            await Data.HGSerializers.SaveTutorsForSpeciesAsync(_species.Name, selected);
        }

        private async void OnPreviewTutors(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var selected = TutorList.SelectedItems
                .Cast<string>()
                .Select(s =>
                {
                    var parts = s.Split(':');
                    var tutor = parts[0].Trim();
                    var rest = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    var moveAndCost = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var move = moveAndCost.Length > 0 ? moveAndCost[0].Trim() : string.Empty;
                    var cost = 0;
                    if (moveAndCost.Length > 1) int.TryParse(moveAndCost[^1], out cost);
                    return (tutor, move, cost);
                })
                .ToList();
            var diff = await Data.HGSerializers.PreviewTutorsAsync(_species.Name, selected);
            await ShowDiffDialog(diff, "tutordata.txt");
        }

        private async void OnSaveOverview(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var ov = CollectOverviewFromUi();
            await Data.HGSerializers.SaveOverviewAsync(_species.Name, ov);
        }

        private async void OnPreviewOverview(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var ov = CollectOverviewFromUi();
            var diff = await Data.HGSerializers.PreviewOverviewAsync(_species.Name, ov);
            await ShowDiffDialog(diff, "mondata.s");
        }

        private Data.HGParsers.SpeciesOverview CollectOverviewFromUi()
        {
            var ov = new Data.HGParsers.SpeciesOverview();
            ov.Type1 = (Type1Box.SelectedItem as string) ?? "TYPE_NORMAL";
            ov.Type2 = (Type2Box.SelectedItem as string) ?? "TYPE_NORMAL";
            int.TryParse(HpBox.Text, out var v); ov.BaseHp = v;
            int.TryParse(AtkBox.Text, out v); ov.BaseAttack = v;
            int.TryParse(DefBox.Text, out v); ov.BaseDefense = v;
            int.TryParse(SpeBox.Text, out v); ov.BaseSpeed = v;
            int.TryParse(SpABox.Text, out v); ov.BaseSpAttack = v;
            int.TryParse(SpDBox.Text, out v); ov.BaseSpDefense = v;
            int.TryParse(EvHpBox.Text, out v); var ehp = v;
            int.TryParse(EvAtkBox.Text, out v); var eatk = v;
            int.TryParse(EvDefBox.Text, out v); var edef = v;
            int.TryParse(EvSpeBox.Text, out v); var espd = v;
            int.TryParse(EvSpABox.Text, out v); var esatk = v;
            int.TryParse(EvSpDBox.Text, out v); var essd = v;
            ov.EvYields = (ehp, eatk, edef, espd, esatk, essd);
            ov.Ability1 = (Ability1Box.SelectedItem as string) ?? "ABILITY_NONE";
            ov.Ability2 = (Ability2Box.SelectedItem as string) ?? "ABILITY_NONE";
            ov.EggGroup1 = (Egg1Box.SelectedItem as string) ?? "EGG_GROUP_NONE";
            ov.EggGroup2 = (Egg2Box.SelectedItem as string) ?? "EGG_GROUP_NONE";
            ov.GrowthRate = (GrowthBox.SelectedItem as string) ?? "GROWTH_MEDIUM_FAST";
            int.TryParse(GenderBox.Text, out v); ov.GenderRatio = v;
            int.TryParse(CatchBox.Text, out v); ov.CatchRate = v;
            int.TryParse(EggCyclesBox.Text, out v); ov.EggCycles = v;
            int.TryParse(FriendBox.Text, out v); ov.BaseFriendship = v;
            ov.Item1 = (Item1Box.SelectedItem as string) ?? "ITEM_NONE";
            ov.Item2 = (Item2Box.SelectedItem as string) ?? "ITEM_NONE";
            return ov;
        }

        private async Task ShowDiffDialog(string diff, string title)
        {
            var dlg = new ContentDialog
            {
                Title = $"Preview changes — {title}",
                PrimaryButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Content = new ScrollViewer
                {
                    Content = new TextBox
                    {
                        Text = string.IsNullOrWhiteSpace(diff) ? "No changes" : diff,
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.NoWrap,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                    }
                }
            };
            await dlg.ShowAsync();
        }

        // Keyboard shortcuts
        private void OnCtrlS(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Save based on current tab header
            var current = Tabs.SelectedItem as TabViewItem;
            var header = (current?.Header as string) ?? string.Empty;
            switch (header)
            {
                case "Overview": OnSaveOverview(this, null!); break;
                case "Level-up Moves": OnSaveLevelUp(this, null!); break;
                case "TM/HM": OnSaveTmHm(this, null!); break;
                case "Evolutions": OnSaveEvolutions(this, null!); break;
                case "Egg Moves": OnSaveEggMoves(this, null!); break;
                case "Tutor Moves": OnSaveTutors(this, null!); break;
            }
            args.Handled = true;
        }

        // Evolution param auto-sync handlers
        private void OnEvolutionMethodChanged(object sender, SelectionChangedEventArgs e)
        {
            // If method is EVO_ITEM, show item picker; else keep numeric box
            if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
            {
                // Nothing else needed; visibility is handled by presence of the ComboBox
                // We keep both controls; item picker changes write a number
            }
        }

        private void OnEvolutionItemChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (!string.Equals(model.Method, "EVO_ITEM", StringComparison.Ordinal)) return;
                    var macro = cb.SelectedItem as string;
                    if (string.IsNullOrEmpty(macro)) return;
                    if (Data.HGParsers.TryGetItemValue(macro, out var id))
                    {
                        model.Param = id;
                    }
                }
            }
            catch { /* swallow to prevent UI crash on template creation */ }
        }

        private void OnEvolutionMoveChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (!string.Equals(model.Method, "EVO_MOVE", StringComparison.Ordinal)) return;
                    var macro = cb.SelectedItem as string;
                    if (string.IsNullOrEmpty(macro)) return;
                    if (Data.HGParsers.TryGetMoveValue(macro, out var id))
                    {
                        model.Param = id;
                    }
                }
            }
            catch { }
        }

        private void OnEvolutionMapChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (!string.Equals(model.Method, "EVO_MAP", StringComparison.Ordinal)) return;
                    var macro = cb.SelectedItem as string;
                    if (string.IsNullOrEmpty(macro)) return;
                    if (Data.HGParsers.TryGetMapValue(macro, out var id))
                    {
                        model.Param = id;
                    }
                }
            }
            catch { }
        }

        private void OnEvolutionFriendTimeChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (!model.Method.StartsWith("EVO_FRIENDSHIP", StringComparison.Ordinal)) return;
                    var val = cb.SelectedItem as string;
                    if (string.IsNullOrEmpty(val)) return;
                    int mapped = 0;
                    switch (val)
                    {
                        case "DAY": mapped = 1; break;
                        case "NIGHT": mapped = 2; break;
                        case "MORNING": mapped = 3; break;
                        case "EVENING": mapped = 4; break;
                        default: mapped = 0; break;
                    }
                    model.Param = mapped;
                }
            }
            catch { }
        }
        private List<string> ValidateLevelUp(List<(int level, string move)> entries)
        {
            var errors = new List<string>();
            var moveSet = new HashSet<string>(Data.HGParsers.MoveMacros);
            for (int i = 0; i < entries.Count; i++)
            {
                var (level, move) = entries[i];
                if (level < 1 || level > 100) errors.Add($"Row {i + 1}: Level must be 1-100.");
                if (string.IsNullOrWhiteSpace(move) || !moveSet.Contains(move)) errors.Add($"Row {i + 1}: Move macro not found.");
            }
            return errors;
        }

        private List<string> ValidateEgg(List<string> moves)
        {
            var errors = new List<string>();
            var moveSet = new HashSet<string>(Data.HGParsers.MoveMacros);
            for (int i = 0; i < moves.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(moves[i]) || !moveSet.Contains(moves[i])) errors.Add($"Row {i + 1}: Move macro not found.");
            }
            return errors;
        }

        private List<string> ValidateEvolutions(List<(string method, int param, string target)> evolutions)
        {
            var errors = new List<string>();
            var methods = new HashSet<string>(Data.HGParsers.EvolutionMethodMacros);
            var species = new HashSet<string>(Data.HGParsers.SpeciesMacroNames);
            for (int i = 0; i < evolutions.Count; i++)
            {
                var (method, param, target) = evolutions[i];
                if (string.IsNullOrWhiteSpace(method) || !methods.Contains(method)) errors.Add($"Row {i + 1}: Unknown evolution method.");
                if (param < 0) errors.Add($"Row {i + 1}: Param must be >= 0.");
                if (string.IsNullOrWhiteSpace(target) || !species.Contains(target)) errors.Add($"Row {i + 1}: Unknown target species.");

                // Method-aware checks
                if (method == "EVO_LEVEL")
                {
                    if (param < 1 || param > 100) errors.Add($"Row {i + 1}: Level must be 1-100 for EVO_LEVEL.");
                }
                if (method == "EVO_ITEM")
                {
                    // Allow either numeric id or ITEM_* macro; we only validate when macro is used and known
                    if (!Data.HGParsers.TryGetItemValue($"ITEM_{param}", out _))
                    {
                        // Can't infer macro from number; skip strict validation
                    }
                }
            }
            return errors;
        }

        private void OnTmSearch(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            var q = (TmSearch.Text ?? string.Empty).Trim();
            _tmhmView = string.IsNullOrEmpty(q) ? _tmhmAll.ToList() : _tmhmAll.Where(x => x.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            TmHmList.ItemsSource = _tmhmView;
        }

        private void OnTutorSearch(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            var q = (TutorSearch.Text ?? string.Empty).Trim();
            _tutorView = string.IsNullOrEmpty(q) ? _tutorAll.ToList() : _tutorAll.Where(x => ($"{x.tutor} {x.move} {x.cost}").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            _tutorViewDisplay = _tutorView.Select(t => $"{t.tutor}: {t.move} {t.cost}").ToList();
            TutorList.ItemsSource = _tutorViewDisplay;
        }
    }
}


