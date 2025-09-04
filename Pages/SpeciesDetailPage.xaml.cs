using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.IO;

namespace HGEngineGUI.Pages
{
    public sealed partial class SpeciesDetailPage : Page
    {
        public IReadOnlyList<string> EvoMethods { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> SpeciesOptions { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> ItemOptions { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> EvoMoveOptions { get; private set; } = Array.Empty<string>();
        public IReadOnlyList<string> MapOptions { get; private set; } = Array.Empty<string>();
        private SpeciesEntry? _species;
        private List<(int level, string move)> _levelUp = new();
        private List<LevelUpEntry> _levelUpModel = new();
        public IReadOnlyList<string> MoveOptions { get; private set; } = Array.Empty<string>();
        private List<string> _egg = new();
        private List<EggMoveEntry> _eggModel = new();
        private List<string> _tmhmAll = new();
        private HashSet<string> _tmhmSelected = new();
        private List<string> _tmhmView = new();
        private List<(string method, int param, string target, int form)> _evolutions = new();
        private List<EvolutionEntry> _evoModel = new();
        private HashSet<string> _tutorSelected = new();
        private List<(string tutor, string move, int cost)> _tutorAll = new();
        private List<(string tutor, string move, int cost)> _tutorView = new();
        private List<string> _tutorViewDisplay = new();

        public SpeciesDetailPage()
        {
            InitializeComponent();
        }

        private void OnLevelUpListLoaded(object sender, RoutedEventArgs e) { }

        private void RenderLevelUpStack()
        {
            if (LevelUpStack == null) return;
            LevelUpStack.Children.Clear();
            foreach (var row in _levelUpModel)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Padding = new Thickness(8) };
                var levelBox = new TextBox { Header = "Level", Width = 120, Text = row.Level.ToString() };
                levelBox.TextChanging += (tb, args) => { if (int.TryParse(levelBox.Text, out var lv)) row.Level = lv; };
                var moveCombo = new ComboBox { Width = 320, IsEditable = true, PlaceholderText = "MOVE_*" };
                moveCombo.ItemsSource = Data.HGParsers.MoveMacros;
                if (!string.IsNullOrWhiteSpace(row.Move) && Data.HGParsers.MoveMacros.Contains(row.Move)) moveCombo.SelectedItem = row.Move;
                moveCombo.Text = row.Move;
                moveCombo.SelectionChanged += (s, e) => { row.Move = (moveCombo.SelectedItem as string) ?? (moveCombo.Text ?? string.Empty); };
                moveCombo.LostFocus += (s, e) => { row.Move = moveCombo.Text ?? string.Empty; };
                var remove = new Button { Content = "Remove" };
                remove.Click += (s, e) => { _levelUpModel.Remove(row); RenderLevelUpStack(); };
                panel.Children.Add(levelBox);
                panel.Children.Add(moveCombo);
                panel.Children.Add(remove);
                LevelUpStack.Children.Add(panel);
            }
        }

        private void OnEvolutionListLoaded(object sender, RoutedEventArgs e) { }

        private void RenderEvolutionsStack()
        {
            if (EvolutionStack == null) return;
            EvolutionStack.Children.Clear();
            int blockIndex = 0;
            foreach (var row in _evoModel)
            {
                // Visual block separator to distinguish multiple evolution lines
                var border = new Border
                {
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(4),
                    Padding = new Thickness(8)
                };
                var outer = new StackPanel { Spacing = 8 };
                border.Child = outer;

                // Row 1: method, param, target
                var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                var methodCombo = new ComboBox
                {
                    Width = 220,
                    IsEditable = true,
                    Header = "Method"
                };
                methodCombo.ItemsSource = Data.HGParsers.EvolutionMethodMacros;
                if (Data.HGParsers.EvolutionMethodMacros.Contains(row.Method)) methodCombo.SelectedItem = row.Method;
                methodCombo.Text = row.Method;
                methodCombo.SelectionChanged += (s, e) => { row.Method = (methodCombo.SelectedItem as string) ?? (methodCombo.Text ?? string.Empty); };
                methodCombo.LostFocus += (s, e) => { row.Method = methodCombo.Text ?? string.Empty; };

                var paramBox = new TextBox
                {
                    Width = 140,
                    Header = "Param",
                    Text = row.Param.ToString()
                };
                paramBox.TextChanging += (tb, args) =>
                {
                    if (int.TryParse(paramBox.Text, out var pv)) row.Param = pv;
                };

                var targetCombo = new ComboBox
                {
                    Width = 220,
                    IsEditable = true,
                    Header = "Target"
                };
                targetCombo.ItemsSource = Data.HGParsers.SpeciesMacroNames;
                if (Data.HGParsers.SpeciesMacroNames.Contains(row.Target)) targetCombo.SelectedItem = row.Target;
                targetCombo.Text = row.Target;
                targetCombo.SelectionChanged += (s, e) => { row.Target = (targetCombo.SelectedItem as string) ?? (targetCombo.Text ?? string.Empty); };
                targetCombo.LostFocus += (s, e) => { row.Target = targetCombo.Text ?? string.Empty; };

                var formBox = new TextBox
                {
                    Width = 100,
                    Header = "Form",
                    Text = row.Form.ToString()
                };
                formBox.TextChanging += (tb, args) => { if (int.TryParse(formBox.Text, out var fv)) row.Form = fv; };

                // Hide old method/param editors; keep only Target and Form in the header
                // top.Children.Add(methodCombo);
                // top.Children.Add(paramBox);
                top.Children.Add(targetCombo);
                top.Children.Add(formBox);
                outer.Children.Add(top);

                // Row 2: remove button only (legacy helpers removed)
                var bottom = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
                var removeBtn = new Button { Content = "Remove" };
                removeBtn.Click += (s, e) => { _evoModel.Remove(row); RenderEvolutionsStack(); };
                bottom.Children.Add(removeBtn);
                outer.Children.Add(bottom);

                // Per-method conditions area
                var condHeader = new TextBlock { Text = "Conditions", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
                outer.Children.Add(condHeader);
                var condList = new StackPanel { Spacing = 6 };
                // Renderer for each condition
                void RenderCondRows()
                {
                    condList.Children.Clear();
                    foreach (var cond in row.Conditions.ToList())
                    {
                        var line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                        var methodC = new ComboBox { Width = 220, IsEditable = true };
                        methodC.ItemsSource = Data.HGParsers.EvolutionMethodMacros;
                        if (!string.IsNullOrWhiteSpace(cond.Method) && Data.HGParsers.EvolutionMethodMacros.Contains(cond.Method)) methodC.SelectedItem = cond.Method;
                        methodC.Text = cond.Method;
                        methodC.SelectionChanged += (s, e) => { cond.Method = (methodC.SelectedItem as string) ?? (methodC.Text ?? string.Empty); };
                        methodC.LostFocus += (s, e) => { cond.Method = methodC.Text ?? string.Empty; };

                        // Dynamic param editor
                        FrameworkElement paramEditor = new TextBox { Width = 140, Header = "Param", Text = cond.Param.ToString() };
                        void RebindParamEditor()
                        {
                            // Default integer box
                            FrameworkElement editor;
                            string m = cond.Method ?? string.Empty;
                            if (m == "EVO_ITEM" || m == "EVO_TRADE_ITEM" || m == "EVO_STONE" || m == "EVO_STONE_MALE" || m == "EVO_STONE_FEMALE" || m == "EVO_ITEM_DAY" || m == "EVO_ITEM_NIGHT")
                            {
                                var cb = new ComboBox { Width = 240, Header = "Item" };
                                var items = Data.HGParsers.ItemMacros.ToList();
                                items.Sort(StringComparer.Ordinal);
                                cb.ItemsSource = items;
                                cb.SelectionChanged += (s, e) => { var macro = cb.SelectedItem as string; if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetItemValue(macro, out var id)) cond.Param = id; };
                                editor = cb;
                            }
                            else if (m == "EVO_HAS_MOVE")
                            {
                                var cb = new ComboBox { Width = 260, Header = "Move" };
                                var moves = Data.HGParsers.MoveMacros.ToList();
                                moves.Sort(StringComparer.Ordinal);
                                cb.ItemsSource = moves;
                                cb.SelectionChanged += (s, e) => { var macro = cb.SelectedItem as string; if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetMoveValue(macro, out var id)) cond.Param = id; };
                                editor = cb;
                            }
                            else if (m == "EVO_HAS_MOVE_TYPE")
                            {
                                var cb = new ComboBox { Width = 220, Header = "Type" };
                                var types = Data.HGParsers.TypeMacros.ToList();
                                types.Sort(StringComparer.Ordinal);
                                cb.ItemsSource = types;
                                cb.SelectionChanged += (s, e) => { var macro = cb.SelectedItem as string; if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetTypeValue(macro, out var id)) cond.Param = id; };
                                editor = cb;
                            }
                            else if (m == "EVO_OTHER_PARTY_MON" || m == "EVO_TRADE_SPECIFIC_MON")
                            {
                                var cb = new ComboBox { Width = 300, Header = "Species" };
                                var species = Data.HGParsers.SpeciesMacroNames.ToList();
                                species.Sort(StringComparer.Ordinal);
                                cb.ItemsSource = species;
                                cb.SelectionChanged += (s, e) => { var macro = cb.SelectedItem as string; if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetSpeciesValue(macro, out var id)) cond.Param = id; };
                                editor = cb;
                            }
                            else if (m == "EVO_CORONET" || m == "EVO_ETERNA" || m == "EVO_ROUTE217")
                            {
                                // These are location checks but param unused; keep a disabled box for clarity
                                var tb = new TextBox { Width = 120, Header = "Param", IsEnabled = false, Text = "0" };
                                editor = tb;
                            }
                            else
                            {
                                var tb = new TextBox { Width = 140, Header = "Param", Text = cond.Param.ToString() };
                                tb.TextChanging += (s, e) => { if (int.TryParse(tb.Text, out var v)) cond.Param = v; };
                                editor = tb;
                            }
                            int idx = line.Children.IndexOf(paramEditor);
                            if (idx >= 0) line.Children.RemoveAt(idx);
                            paramEditor = editor;
                            line.Children.Insert(1, paramEditor);
                        }

                        methodC.SelectionChanged += (s, e) => RebindParamEditor();
                        line.Children.Add(methodC);
                        line.Children.Add(paramEditor);
                        condList.Children.Add(line);

                        // Initial bind based on current method
                        RebindParamEditor();
                    }
                }
                RenderCondRows();
                outer.Children.Add(condList);

                var addCond = new Button { Content = "Add Method" };
                addCond.Click += (s, e) =>
                {
                    // Create a new evolution block (entry) instead of adding a condition to the current block
                    var entry = new EvolutionEntry { Method = "EVO_LEVEL", Param = 1, Target = string.Empty, Form = 0 };
                    entry.Conditions.Add(new EvoCondition { Method = "EVO_LEVEL", Param = 1 });
                    _evoModel.Add(entry);
                    RenderEvolutionsStack();
                };
                outer.Children.Add(addCond);

                // Remove the legacy, top-level method UI: replace header label to make intent clear
                var headerText = new TextBlock { Text = $"Evolution #{++blockIndex}", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
                outer.Children.Insert(0, headerText);

                EvolutionStack.Children.Add(border);
            }
            try { StatusText.Text = $"Evo rows (manual): {_evoModel.Count}"; } catch { }
        }

        private void OnEvolutionStackLoaded(object sender, RoutedEventArgs e)
        {
            try { RenderEvolutionsStack(); } catch (Exception ex) { StatusText.Text = ex.Message; }
        }

        private void SyncLevelUpFromUi()
        {
            _levelUpModel = _levelUpModel
                .Select(m => new LevelUpEntry { Level = m.Level, Move = (m.Move ?? string.Empty).Trim() })
                .OrderBy(m => m.Level)
                .ToList();
        }

        private void SyncEggFromUi()
        {
            _eggModel = _eggModel
                .Select(m => new EggMoveEntry { Move = (m.Move ?? string.Empty).Trim() })
                .Where(m => !string.IsNullOrWhiteSpace(m.Move))
                .ToList();
        }

        private void SyncEvolutionsFromUi()
        {
            foreach (var e in _evoModel)
            {
                e.Method = (e.Method ?? string.Empty).Trim();
                e.Target = (e.Target ?? string.Empty).Trim();
            }
        }
        private void OnEggMovesListLoaded(object sender, RoutedEventArgs e) { }

        private void RenderEggStack()
        {
            if (EggStack == null) return;
            EggStack.Children.Clear();
            foreach (var row in _eggModel)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Padding = new Thickness(8) };
                var moveCombo = new ComboBox { Width = 320, IsEditable = true, PlaceholderText = "MOVE_*" };
                moveCombo.ItemsSource = Data.HGParsers.MoveMacros;
                if (!string.IsNullOrWhiteSpace(row.Move) && Data.HGParsers.MoveMacros.Contains(row.Move)) moveCombo.SelectedItem = row.Move;
                moveCombo.Text = row.Move;
                moveCombo.SelectionChanged += (s, e) => { row.Move = (moveCombo.SelectedItem as string) ?? (moveCombo.Text ?? string.Empty); };
                moveCombo.LostFocus += (s, e) => { row.Move = moveCombo.Text ?? string.Empty; };

                var remove = new Button { Content = "Remove" };
                remove.Click += (s, e) => { _eggModel.Remove(row); RenderEggStack(); };

                panel.Children.Add(moveCombo);
                panel.Children.Add(remove);
                EggStack.Children.Add(panel);
            }
        }

        // Ensure initial move text renders even if SelectedItem isn't resolved yet
        private void OnMoveComboLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb)
                {
                    // Always populate ItemsSource at load time to avoid template timing issues
                    if (cb.ItemsSource == null)
                    {
                        cb.ItemsSource = Data.HGParsers.MoveMacros;
                    }
                    if (cb.DataContext is LevelUpEntry lu && !string.IsNullOrWhiteSpace(lu.Move))
                    {
                        if (cb.ItemsSource is IEnumerable<string> moves && moves.Contains(lu.Move))
                        {
                            cb.SelectedItem = lu.Move;
                        }
                        cb.Text = lu.Move;
                    }
                    else if (cb.DataContext is EggMoveEntry em && !string.IsNullOrWhiteSpace(em.Move))
                    {
                        if (cb.ItemsSource == null)
                        {
                            cb.ItemsSource = Data.HGParsers.MoveMacros;
                        }
                        if (cb.ItemsSource is IEnumerable<string> moves2 && moves2.Contains(em.Move))
                        {
                            cb.SelectedItem = em.Move;
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
            // Ensure macro caches exist; if not, load them
            if (!Data.HGParsers.TypeMacros.Any() || !Data.HGParsers.AbilityMacros.Any() || !Data.HGParsers.EggGroupMacros.Any() || !Data.HGParsers.GrowthRateMacros.Any() || !Data.HGParsers.ItemMacros.Any())
            {
                await Data.HGParsers.RefreshCachesAsync();
            }
            // Populate static resources at runtime so templates have data before they render
            // no-op
            // Bind dropdown sources explicitly so they are populated even if page loaded before caches
            Type1Box.ItemsSource = Data.HGParsers.TypeMacros;
            Type2Box.ItemsSource = Data.HGParsers.TypeMacros;
            var abilityList = Data.HGParsers.AbilityMacros.ToList();
            abilityList.Sort(StringComparer.Ordinal);
            Ability1Box.ItemsSource = abilityList;
            Ability2Box.ItemsSource = abilityList;
            AbilityHiddenBox.ItemsSource = abilityList;
            Egg1Box.ItemsSource = Data.HGParsers.EggGroupMacros;
            Egg2Box.ItemsSource = Data.HGParsers.EggGroupMacros;
            GrowthBox.ItemsSource = Data.HGParsers.GrowthRateMacros;
            Item1Box.ItemsSource = Data.HGParsers.ItemMacros;
            Item2Box.ItemsSource = Data.HGParsers.ItemMacros;
            // Snapshot move list for summaries/editors
            MoveOptions = Data.HGParsers.MoveMacros.ToList();
            _levelUp = Data.HGParsers.LevelUpMoves.ToList();
            _levelUpModel = _levelUp.Select(m => new LevelUpEntry { Level = m.level, Move = m.move }).ToList();
            RenderLevelUpStack();
            _evolutions = Data.HGParsers.Evolutions.ToList();
            _evoModel = _evolutions.Select(e =>
            {
                var en = new EvolutionEntry { Method = e.method, Param = e.param, Target = e.target, Form = e.form };
                // Ensure each loaded evolution has at least one blank condition for UI consistency
                en.Conditions.Add(new EvoCondition { Method = string.Empty, Param = 1 });
                return en;
            }).ToList();
            RenderEvolutionsStack();
            _egg = Data.HGParsers.EggMoves.ToList();
            _eggModel = _egg.Select(m => new EggMoveEntry { Move = m }).ToList();
            RenderEggStack();
            try { StatusText.Text = $"Loaded: {_levelUpModel.Count} level-up, {_evoModel.Count} evolutions, {_eggModel.Count} egg moves."; } catch { }
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
                AbilityHiddenBox.SelectedItem = ov.AbilityHidden;
                Egg1Box.SelectedItem = ov.EggGroup1;
                Egg2Box.SelectedItem = ov.EggGroup2;
                GrowthBox.SelectedItem = ov.GrowthRate;
                GenderBox.Text = ov.GenderRatio.ToString();
                CatchBox.Text = ov.CatchRate.ToString();
                BaseExpBox.Text = ov.BaseExp.ToString();
                EggCyclesBox.Text = ov.EggCycles.ToString();
                FriendBox.Text = ov.BaseFriendship.ToString();
                Item1Box.SelectedItem = ov.Item1;
                Item2Box.SelectedItem = ov.Item2;
                RunChanceBox.Text = ov.RunChance.ToString();
                DexClassBox.Text = ov.DexClassification ?? string.Empty;
                DexEntryBox.Text = ov.DexEntry ?? string.Empty;
                DexHeightBox.Text = ov.DexHeight ?? string.Empty;
                DexWeightBox.Text = ov.DexWeight ?? string.Empty;
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
                    RenderLevelUpStack();
                }
            }
        }

        private void OnDeleteLevelUp(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // With ItemsControl, provide row-level Remove buttons instead
        }

        private async void OnSaveLevelUp(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            SyncLevelUpFromUi();
            var entries = _levelUpModel.Select(m => (m.Level, m.Move)).OrderBy(m => m.Level).ToList();
            var errors = ValidateLevelUp(entries);
            if (errors.Count > 0)
            {
                await ShowDiffDialog(string.Join("\n", errors), "Fix validation errors");
                return;
            }
            var path = Data.HGParsers.PathLevelUp ?? Path.Combine(ProjectContext.RootPath ?? string.Empty, "armips", "data", "levelupdata.s");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { try { StatusText.Text = $"File not found: levelupdata.s"; } catch { } return; }
            await Data.HGSerializers.SaveLevelUpAsync(_species.Name, entries);
            try { StatusText.Text = $"Saved level-up moves ({entries.Count}) -> {path}"; } catch { }
        }

        private async void OnPreviewLevelUp(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            SyncLevelUpFromUi();
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
                    RenderEggStack();
                }
            }
        }

        private void OnDeleteEggMove(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // With ItemsControl, provide row-level Remove buttons instead
        }

        private async void OnSaveEggMoves(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            SyncEggFromUi();
            var moves = _eggModel.Select(m => m.Move).ToList();
            var errors = ValidateEgg(moves);
            if (errors.Count > 0)
            {
                await ShowDiffDialog(string.Join("\n", errors), "Fix validation errors");
                return;
            }
            var path = Data.HGParsers.PathEgg ?? Path.Combine(ProjectContext.RootPath ?? string.Empty, "armips", "data", "eggmoves.s");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { try { StatusText.Text = $"File not found: eggmoves.s"; } catch { } return; }
            await Data.HGSerializers.SaveEggMovesAsync(_species.Name, moves);
            try { StatusText.Text = $"Saved egg moves ({moves.Count}) -> {path}"; } catch { }
        }

        private async void OnPreviewEggMoves(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            SyncEggFromUi();
            var diff = await Data.HGSerializers.PreviewEggMovesAsync(_species.Name, _eggModel.Select(m=>m.Move).ToList());
            await ShowDiffDialog(diff, "eggmoves.s");
        }
        private async void OnAddEvolution(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var entry = new EvolutionEntry
            {
                Method = "EVO_LEVEL",
                Param = 1,
                Target = string.Empty,
                Form = 0
            };
            entry.Conditions.Add(new EvoCondition { Method = string.Empty, Param = 1 });
            _evoModel.Add(entry);
                    RenderEvolutionsStack();
        }

        private void OnDeleteEvolution(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // With ItemsControl, provide row-level Remove buttons instead
        }

        private void OnRemoveLevelUpRow(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is LevelUpEntry row)
            {
                _levelUpModel.Remove(row);
                RenderLevelUpStack();
            }
        }

        private void OnRemoveEggRow(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is EggMoveEntry row)
            {
                _eggModel.Remove(row);
                RenderEggStack();
            }
        }

        private void OnRemoveEvolutionRow(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is EvolutionEntry row)
            {
                _evoModel.Remove(row);
                RenderEvolutionsStack();
            }
        }

        private async void OnSaveEvolutions(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            SyncEvolutionsFromUi();
            var current = FlattenEvolutions();
            var errors = ValidateEvolutions(current);
            if (errors.Count > 0)
            {
                await ShowDiffDialog(string.Join("\n", errors), "Fix validation errors");
                return;
            }
            var path = Data.HGParsers.PathEvo ?? Path.Combine(ProjectContext.RootPath ?? string.Empty, "armips", "data", "evodata.s");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { try { StatusText.Text = $"File not found: evodata.s"; } catch { } return; }
            await Data.HGSerializers.SaveEvolutionsAsync(_species.Name, current);
            try { StatusText.Text = $"Saved evolutions ({current.Count}) -> {path}"; } catch { }
        }

        private async void OnPreviewEvolutions(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            SyncEvolutionsFromUi();
            var diff = await Data.HGSerializers.PreviewEvolutionsAsync(_species.Name, FlattenEvolutions());
            await ShowDiffDialog(diff, "evodata.s");
        }

        private async void OnSaveTmHm(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_species == null) return;
            var selected = TmHmList.SelectedItems.Cast<string>().ToList();
            var path = Data.HGParsers.PathTm ?? Path.Combine(ProjectContext.RootPath ?? string.Empty, "armips", "data", "tmlearnset.txt");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { try { StatusText.Text = $"File not found: tmlearnset.txt"; } catch { } return; }
            await Data.HGSerializers.SaveTmHmForSpeciesAsync(_species.Name, selected);
            try { StatusText.Text = $"Saved TM/HM selections ({selected.Count}) -> {path}"; } catch { }
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
            var path = Data.HGParsers.PathTutor ?? Path.Combine(ProjectContext.RootPath ?? string.Empty, "armips", "data", "tutordata.txt");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { try { StatusText.Text = $"File not found: tutordata.txt"; } catch { } return; }
            await Data.HGSerializers.SaveTutorsForSpeciesAsync(_species.Name, selected);
            try { StatusText.Text = $"Saved tutors ({selected.Count}) -> {path}"; } catch { }
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
            var path = Data.HGParsers.PathMondata ?? Path.Combine(ProjectContext.RootPath ?? string.Empty, "armips", "data", "mondata.s");
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) { try { StatusText.Text = $"File not found: mondata.s"; } catch { } return; }
            await Data.HGSerializers.SaveOverviewAsync(_species.Name, ov);
            // Persist Base Exp to data/BaseExperienceTable.c
            await Data.HGSerializers.SaveBaseExpAsync(_species.Name, ov.BaseExp);
            // Also persist Hidden Ability via HiddenAbilityTable.c
            var hidden = (AbilityHiddenBox.SelectedItem as string) ?? ov.AbilityHidden;
            if (!string.IsNullOrWhiteSpace(hidden))
            {
                await Data.HGSerializers.SaveHiddenAbilityAsync(_species.Name, hidden);
            }
            try { StatusText.Text = $"Saved overview -> {path}"; } catch { }
        }

        private async void OnPreviewOverview(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var ov = CollectOverviewFromUi();
            var diff = await Data.HGSerializers.PreviewOverviewAsync(_species.Name, ov);
            await ShowDiffDialog(diff, "mondata.s");
        }

        private async void OnSaveHiddenAbility(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var ability = (AbilityHiddenBox.SelectedItem as string) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ability)) return;
            await Data.HGSerializers.SaveHiddenAbilityAsync(_species.Name, ability);
        }

        private async void OnPreviewHiddenAbility(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var ability = (AbilityHiddenBox.SelectedItem as string) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ability)) return;
            var diff = await Data.HGSerializers.PreviewHiddenAbilityAsync(_species.Name, ability);
            await ShowDiffDialog(diff, "HiddenAbilityTable.c");
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
            ov.AbilityHidden = (AbilityHiddenBox.SelectedItem as string) ?? "ABILITY_NONE";
            ov.EggGroup1 = (Egg1Box.SelectedItem as string) ?? "EGG_GROUP_NONE";
            ov.EggGroup2 = (Egg2Box.SelectedItem as string) ?? "EGG_GROUP_NONE";
            ov.GrowthRate = (GrowthBox.SelectedItem as string) ?? "GROWTH_MEDIUM_FAST";
            int.TryParse(GenderBox.Text, out v); ov.GenderRatio = v;
            int.TryParse(CatchBox.Text, out v); ov.CatchRate = v;
            int.TryParse(BaseExpBox.Text, out v); ov.BaseExp = v;
            int.TryParse(EggCyclesBox.Text, out v); ov.EggCycles = v;
            int.TryParse(FriendBox.Text, out v); ov.BaseFriendship = v;
            ov.Item1 = (Item1Box.SelectedItem as string) ?? "ITEM_NONE";
            ov.Item2 = (Item2Box.SelectedItem as string) ?? "ITEM_NONE";
            int.TryParse(RunChanceBox.Text, out v); ov.RunChance = v;
            ov.DexClassification = DexClassBox.Text ?? string.Empty;
            ov.DexEntry = DexEntryBox.Text ?? string.Empty;
            ov.DexHeight = DexHeightBox.Text ?? string.Empty;
            ov.DexWeight = DexWeightBox.Text ?? string.Empty;
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
        private void OnEvoMethodLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (cb.ItemsSource == null) cb.ItemsSource = Data.HGParsers.EvolutionMethodMacros;
                    if (cb.SelectedItem == null && cb.ItemsSource is IEnumerable<string> methods && !string.IsNullOrWhiteSpace(model.Method) && methods.Contains(model.Method))
                    {
                        cb.SelectedItem = model.Method;
                    }
                    cb.Text = model.Method;
                }
            }
            catch { }
        }

        private void OnEvoTargetLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (cb.ItemsSource == null) cb.ItemsSource = Data.HGParsers.SpeciesMacroNames;
                    if (cb.SelectedItem == null && cb.ItemsSource is IEnumerable<string> species && !string.IsNullOrWhiteSpace(model.Target) && species.Contains(model.Target))
                    {
                        cb.SelectedItem = model.Target;
                    }
                    cb.Text = model.Target;
                }
            }
            catch { }
        }
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

        private void OnEvoItemLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (cb.ItemsSource == null) cb.ItemsSource = Data.HGParsers.ItemMacros;
                    // If current method is EVO_ITEM, reflect item's numeric param back to macro text for display when possible
                    if (string.Equals(model.Method, "EVO_ITEM", StringComparison.Ordinal))
                    {
                        // We cannot invert number->macro reliably without a reverse map here; rely on Text showing macro if SelectedItem exists
                        // If Param was set via helper earlier, SelectedItem will be set via SelectionChanged handler
                    }
                }
            }
            catch { }
        }

        private void OnEvoMoveLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (cb.ItemsSource == null) cb.ItemsSource = Data.HGParsers.MoveMacros;
                    if (string.Equals(model.Method, "EVO_MOVE", StringComparison.Ordinal))
                    {
                        cb.Text = cb.SelectedItem as string ?? cb.Text;
                    }
                }
            }
            catch { }
        }

        private void OnEvoMapLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.DataContext is EvolutionEntry model)
                {
                    if (cb.ItemsSource == null) cb.ItemsSource = Data.HGParsers.MapMacros;
                    if (string.Equals(model.Method, "EVO_MAP", StringComparison.Ordinal))
                    {
                        cb.Text = cb.SelectedItem as string ?? cb.Text;
                    }
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

        private List<string> ValidateEvolutions(List<(string method, int param, string target, int form)> evolutions)
        {
            var errors = new List<string>();
            var methods = new HashSet<string>(Data.HGParsers.EvolutionMethodMacros);
            var species = new HashSet<string>(Data.HGParsers.SpeciesMacroNames);
            for (int i = 0; i < evolutions.Count; i++)
            {
                var (method, param, target, form) = evolutions[i];
                if (string.IsNullOrWhiteSpace(method) || !methods.Contains(method)) errors.Add($"Row {i + 1}: Unknown evolution method.");
                if (param < 0) errors.Add($"Row {i + 1}: Param must be >= 0.");
                if (string.IsNullOrWhiteSpace(target) || !species.Contains(target)) errors.Add($"Row {i + 1}: Unknown target species.");
                if (form < 0 || form > 31) errors.Add($"Row {i + 1}: Form must be 0-31.");

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

        private List<(string method, int param, string target, int form)> FlattenEvolutions()
        {
            var list = new List<(string method, int param, string target, int form)>();
            foreach (var e in _evoModel)
            {
                if (e.Conditions != null && e.Conditions.Count > 0)
                {
                    foreach (var c in e.Conditions)
                    {
                        var method = (c.Method ?? string.Empty).Trim();
                        list.Add((method, c.Param, (e.Target ?? string.Empty).Trim(), e.Form));
                    }
                }
                else
                {
                    list.Add(((e.Method ?? string.Empty).Trim(), e.Param, (e.Target ?? string.Empty).Trim(), e.Form));
                }
            }
            return list;
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


