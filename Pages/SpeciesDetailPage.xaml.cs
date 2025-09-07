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
        private List<string> _tmhmViewDisplay = new();
        private System.Collections.Generic.Dictionary<string, string> _tmhmHeaderToDisplay = new(System.StringComparer.Ordinal);
        private System.Collections.Generic.Dictionary<string, string> _tmhmDisplayToHeader = new(System.StringComparer.OrdinalIgnoreCase);
        private List<(string method, int param, string target, int form)> _evolutions = new();
        private List<EvolutionEntry> _evoModel = new();
        private HashSet<string> _tutorSelected = new();
        private List<(string tutor, string move, int cost)> _tutorAll = new();
        private List<(string tutor, string move, int cost)> _tutorView = new();
        private List<string> _tutorViewDisplay = new();
        private List<string> _abilityIndex = new();
        private List<string> _moveIndex = new();
        private Dictionary<string, string> _tutorHeaderToDisplay = new(StringComparer.Ordinal);
        private Dictionary<string, string> _tutorDisplayToHeader = new(StringComparer.OrdinalIgnoreCase);
        private List<string> _itemIndex = new();

        public SpeciesDetailPage()
        {
            InitializeComponent();
        }

        private static string FormatDisplay(string macro)
        {
            try
            {
                string s = macro ?? string.Empty;
                string[] prefixes = new[] { "SPECIES_", "TYPE_", "ABILITY_", "EGG_GROUP_", "GROWTH_", "ITEM_", "EVO_", "MAP_", "TUTOR_" };
                foreach (var p in prefixes)
                {
                    if (s.StartsWith(p, StringComparison.Ordinal)) { s = s.Substring(p.Length); break; }
                }
                s = s.Replace('_', ' ');
                return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
            }
            catch { return macro; }
        }

        private string MapAbilityToMacro(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return "ABILITY_NONE";
            var match = _abilityIndex.FirstOrDefault(a => string.Equals(FormatDisplay(a), display.Trim(), StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match) ? "ABILITY_NONE" : match;
        }

        private string MapItemToMacro(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return "ITEM_NONE";
            var match = _itemIndex.FirstOrDefault(i => string.Equals(FormatDisplay(i), display.Trim(), StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match) ? "ITEM_NONE" : match;
        }

        private static string FormatMove(string moveMacro)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(moveMacro)) return moveMacro;
                var s = moveMacro.StartsWith("MOVE_", StringComparison.Ordinal) ? moveMacro.Substring(5) : moveMacro;
                s = s.Replace('_', ' ');
                return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
            }
            catch { return moveMacro; }
        }

        private string MapMoveDisplayToMacro(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return string.Empty;
            var match = _moveIndex.FirstOrDefault(m => string.Equals(FormatMove(m), display.Trim(), StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match) ? display : match;
        }

        private string MapSpeciesDisplayToMacro(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return string.Empty;
            var list = Data.HGParsers.SpeciesMacroNames;
            var match = list.FirstOrDefault(m => string.Equals(FormatDisplay(m), display.Trim(), StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match) ? display : match;
        }

        private string MapTypeDisplayToMacro(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return string.Empty;
            var list = Data.HGParsers.TypeMacros;
            var match = list.FirstOrDefault(m => string.Equals(FormatDisplay(m), display.Trim(), StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match) ? display : match;
        }

        private string MapEvoMethodDisplayToMacro(string display)
        {
            if (string.IsNullOrWhiteSpace(display)) return string.Empty;
            var list = Data.HGParsers.EvolutionMethodMacros;
            var match = list.FirstOrDefault(m => string.Equals(FormatDisplay(m), display.Trim(), StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrEmpty(match) ? display : match;
        }

        private static string DisplayTmHeader(string header)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(header)) return header;
                int idx = header.IndexOf("MOVE_", StringComparison.Ordinal);
                if (idx < 0) return header;
                string prefix = header.Substring(0, idx).TrimEnd();
                string moveMacro = header.Substring(idx).Trim();
                return ($"{prefix} {FormatMove(moveMacro)}").Trim();
            }
            catch { return header; }
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

                // Friendly-name hybrid suggestion for moves
                var moveBox = new AutoSuggestBox { Width = 260, Height = 36, PlaceholderText = "Move" };
                moveBox.Text = FormatMove(row.Move);
                moveBox.TextChanged += (sender, args) =>
                {
                    try
                    {
                        if (!args.Reason.Equals(AutoSuggestionBoxTextChangeReason.UserInput)) return;
                        var q = (moveBox.Text ?? string.Empty).Trim();
                        IEnumerable<string> pool = _moveIndex;
                        if (!string.IsNullOrEmpty(q))
                        {
                            pool = _moveIndex.Where(m => FormatMove(m).Contains(q, StringComparison.OrdinalIgnoreCase) || m.Contains(q, StringComparison.OrdinalIgnoreCase));
                        }
                        moveBox.ItemsSource = pool.Select(FormatMove).ToList();
                    }
                    catch { }
                };
                moveBox.SuggestionChosen += (sender, args) =>
                {
                    try
                    {
                        var display = args.SelectedItem as string ?? moveBox.Text;
                        var macro = MapMoveDisplayToMacro(display);
                        row.Move = macro;
                        moveBox.Text = FormatMove(macro);
                    }
                    catch { }
                };
                moveBox.LostFocus += (sender, args) =>
                {
                    try { var macro = MapMoveDisplayToMacro(moveBox.Text); row.Move = macro; moveBox.Text = FormatMove(macro); } catch { }
                };
                var dropdown = new Button { Width = 32, Height = 32 };
                dropdown.Content = new FontIcon { Glyph = "\uE70D" };
                dropdown.Click += (s, e) =>
                {
                    try { moveBox.ItemsSource = _moveIndex.Select(FormatMove).ToList(); moveBox.IsSuggestionListOpen = true; } catch { }
                };

                var remove = new Button { Content = "Remove" };
                remove.Click += (s, e) => { _levelUpModel.Remove(row); RenderLevelUpStack(); };
                panel.Children.Add(levelBox);
                var movePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                movePanel.Children.Add(moveBox);
                movePanel.Children.Add(dropdown);
                panel.Children.Add(movePanel);
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
                var evoMethodsDisplay = Data.HGParsers.EvolutionMethodMacros.Select(FormatDisplay).ToList();
                methodCombo.ItemsSource = evoMethodsDisplay;
                var curMethodDisp = FormatDisplay(row.Method);
                if (evoMethodsDisplay.Contains(curMethodDisp)) methodCombo.SelectedItem = curMethodDisp;
                methodCombo.Text = curMethodDisp;
                methodCombo.SelectionChanged += (s, e) => { row.Method = MapEvoMethodDisplayToMacro((methodCombo.SelectedItem as string) ?? (methodCombo.Text ?? string.Empty)); };
                methodCombo.LostFocus += (s, e) => { row.Method = MapEvoMethodDisplayToMacro(methodCombo.Text ?? string.Empty); };

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
                var speciesDisplay = Data.HGParsers.SpeciesMacroNames.Select(FormatDisplay).ToList();
                targetCombo.ItemsSource = speciesDisplay;
                var curTargetDisp = FormatDisplay(row.Target);
                if (speciesDisplay.Contains(curTargetDisp)) targetCombo.SelectedItem = curTargetDisp;
                targetCombo.Text = curTargetDisp;
                targetCombo.SelectionChanged += (s, e) => { row.Target = MapSpeciesDisplayToMacro((targetCombo.SelectedItem as string) ?? (targetCombo.Text ?? string.Empty)); };
                targetCombo.LostFocus += (s, e) => { row.Target = MapSpeciesDisplayToMacro(targetCombo.Text ?? string.Empty); };

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
                        var methodsDisp = Data.HGParsers.EvolutionMethodMacros.Select(FormatDisplay).ToList();
                        methodC.ItemsSource = methodsDisp;
                        var curDisp = FormatDisplay(cond.Method);
                        if (!string.IsNullOrWhiteSpace(cond.Method) && methodsDisp.Contains(curDisp)) methodC.SelectedItem = curDisp;
                        methodC.Text = curDisp;
                        methodC.SelectionChanged += (s, e) => { cond.Method = MapEvoMethodDisplayToMacro((methodC.SelectedItem as string) ?? (methodC.Text ?? string.Empty)); };
                        methodC.LostFocus += (s, e) => { cond.Method = MapEvoMethodDisplayToMacro(methodC.Text ?? string.Empty); };

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
                                cb.ItemsSource = items.Select(FormatDisplay).ToList();
                                cb.SelectionChanged += (s, e) => { var disp = cb.SelectedItem as string; var macro = Data.HGParsers.ItemMacros.FirstOrDefault(im => string.Equals(FormatDisplay(im), disp, StringComparison.OrdinalIgnoreCase)) ?? disp; if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetItemValue(macro, out var id)) cond.Param = id; };
                                editor = cb;
                            }
                            else if (m == "EVO_HAS_MOVE")
                            {
                                var cb = new ComboBox { Width = 260, Header = "Move" };
                                var moves = Data.HGParsers.MoveMacros.ToList();
                                moves.Sort(StringComparer.Ordinal);
                                cb.ItemsSource = moves.Select(FormatMove).ToList();
                                cb.SelectionChanged += (s, e) => { var disp = cb.SelectedItem as string; var macro = MapMoveDisplayToMacro(disp ?? string.Empty); if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetMoveValue(macro, out var id)) cond.Param = id; };
                                editor = cb;
                            }
                            else if (m == "EVO_HAS_MOVE_TYPE")
                            {
                                var cb = new ComboBox { Width = 220, Header = "Type" };
                                var types = Data.HGParsers.TypeMacros.ToList();
                                types.Sort(StringComparer.Ordinal);
                                cb.ItemsSource = types.Select(FormatDisplay).ToList();
                                cb.SelectionChanged += (s, e) => { var disp = cb.SelectedItem as string; var macro = Data.HGParsers.TypeMacros.FirstOrDefault(tm => string.Equals(FormatDisplay(tm), disp, StringComparison.OrdinalIgnoreCase)) ?? disp; if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetTypeValue(macro, out var id)) cond.Param = id; };
                                editor = cb;
                            }
                            else if (m == "EVO_OTHER_PARTY_MON" || m == "EVO_TRADE_SPECIFIC_MON")
                            {
                                var cb = new ComboBox { Width = 300, Header = "Species" };
                                var species = Data.HGParsers.SpeciesMacroNames.ToList();
                                species.Sort(StringComparer.Ordinal);
                                cb.ItemsSource = species.Select(FormatDisplay).ToList();
                                cb.SelectionChanged += (s, e) => { var disp = cb.SelectedItem as string; var macro = Data.HGParsers.SpeciesMacroNames.FirstOrDefault(sm => string.Equals(FormatDisplay(sm), disp, StringComparison.OrdinalIgnoreCase)) ?? disp; if (!string.IsNullOrEmpty(macro) && Data.HGParsers.TryGetSpeciesValue(macro, out var id)) cond.Param = id; };
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
                .Select(m => new LevelUpEntry { Level = m.Level, Move = MapMoveDisplayToMacro((m.Move ?? string.Empty).Trim()) })
                .OrderBy(m => m.Level)
                .ToList();
        }

        private void SyncEggFromUi()
        {
            _eggModel = _eggModel
                .Select(m => new EggMoveEntry { Move = MapMoveDisplayToMacro((m.Move ?? string.Empty).Trim()) })
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
                var moveBox = new AutoSuggestBox { Width = 260, Height = 36, PlaceholderText = "Move" };
                moveBox.Text = FormatMove(row.Move);
                moveBox.TextChanged += (sender, args) =>
                {
                    try
                    {
                        if (!args.Reason.Equals(AutoSuggestionBoxTextChangeReason.UserInput)) return;
                        var q = (moveBox.Text ?? string.Empty).Trim();
                        IEnumerable<string> pool = _moveIndex;
                        if (!string.IsNullOrEmpty(q))
                        {
                            pool = _moveIndex.Where(m => FormatMove(m).Contains(q, StringComparison.OrdinalIgnoreCase) || m.Contains(q, StringComparison.OrdinalIgnoreCase));
                        }
                        moveBox.ItemsSource = pool.Select(FormatMove).ToList();
                    }
                    catch { }
                };
                moveBox.SuggestionChosen += (sender, args) =>
                {
                    try
                    {
                        var display = args.SelectedItem as string ?? moveBox.Text;
                        var macro = MapMoveDisplayToMacro(display);
                        row.Move = macro;
                        moveBox.Text = FormatMove(macro);
                    }
                    catch { }
                };
                moveBox.LostFocus += (sender, args) =>
                {
                    try { var macro = MapMoveDisplayToMacro(moveBox.Text); row.Move = macro; moveBox.Text = FormatMove(macro); } catch { }
                };
                var dropdown = new Button { Width = 32, Height = 32 };
                dropdown.Content = new FontIcon { Glyph = "\uE70D" };
                dropdown.Click += (s, e) =>
                {
                    try { moveBox.ItemsSource = _moveIndex.Select(FormatMove).ToList(); moveBox.IsSuggestionListOpen = true; } catch { }
                };

                var remove = new Button { Content = "Remove" };
                remove.Click += (s, e) => { _eggModel.Remove(row); RenderEggStack(); };

                var movePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
                movePanel.Children.Add(moveBox);
                movePanel.Children.Add(dropdown);
                panel.Children.Add(movePanel);
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

            Title.Text = FormatDisplay(_species.Name);
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
            Type1Box.ItemsSource = Data.HGParsers.TypeMacros.Select(FormatDisplay).ToList();
            Type2Box.ItemsSource = Data.HGParsers.TypeMacros.Select(FormatDisplay).ToList();
            var abilityList = Data.HGParsers.AbilityMacros.ToList();
            abilityList.Sort(StringComparer.Ordinal);
            _abilityIndex = abilityList;
            Egg1Box.ItemsSource = Data.HGParsers.EggGroupMacros.Select(FormatDisplay).ToList();
            Egg2Box.ItemsSource = Data.HGParsers.EggGroupMacros.Select(FormatDisplay).ToList();
            GrowthBox.ItemsSource = Data.HGParsers.GrowthRateMacros.Select(FormatDisplay).ToList();
            Item1Box.ItemsSource = Data.HGParsers.ItemMacros.Select(FormatDisplay).ToList();
            Item2Box.ItemsSource = Data.HGParsers.ItemMacros.Select(FormatDisplay).ToList();
            var itemsList = Data.HGParsers.ItemMacros.ToList();
            itemsList.Sort(StringComparer.Ordinal);
            _itemIndex = itemsList;
            // Snapshot move list for summaries/editors
            MoveOptions = Data.HGParsers.MoveMacros.ToList();
            _moveIndex = MoveOptions.ToList();
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
            _tutorHeaderToDisplay.Clear();
            _tutorDisplayToHeader.Clear();
            _tutorViewDisplay = _tutorView
                .Select(t =>
                {
                    var disp = $"{FormatDisplay(t.tutor)}: {FormatMove(t.move)} {t.cost}";
                    var header = $"{t.tutor}: {t.move} {t.cost}";
                    _tutorHeaderToDisplay[header] = disp;
                    if (!_tutorDisplayToHeader.ContainsKey(disp)) _tutorDisplayToHeader[disp] = header;
                    return disp;
                })
                .ToList();
            TutorList.ItemsSource = _tutorViewDisplay;
            TutorList.SelectedItems.Clear();
            // Preselect using display strings converted from backend selection set
            var selectedDisplay = Data.HGParsers.TmHmSelectedForSpecies
                .Select(h =>
                {
                    // h format: TUTOR_X: MOVE_MACRO cost
                    var parts = h.Split(':');
                    var tutor = parts[0].Trim();
                    var rest = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                    var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var move = tokens.Length > 0 ? tokens[0].Trim() : string.Empty;
                    var cost = tokens.Length > 1 ? tokens[^1] : "0";
                    return $"{FormatDisplay(tutor)}: {FormatMove(move)} {cost}";
                }).ToHashSet(StringComparer.Ordinal);
            foreach (var header in _tutorView)
            {
                var disp = $"{FormatDisplay(header.tutor)}: {FormatMove(header.move)} {header.cost}";
                if (selectedDisplay.Contains(disp)) TutorList.SelectedItems.Add(disp);
            }

            // TM/HM list: show friendly labels but keep header mapping
            _tmhmAll = Data.HGParsers.TmHmMoves.ToList();
            _tmhmHeaderToDisplay.Clear();
            _tmhmDisplayToHeader.Clear();
            foreach (var h in _tmhmAll)
            {
                var d = DisplayTmHeader(h);
                _tmhmHeaderToDisplay[h] = d;
                if (!_tmhmDisplayToHeader.ContainsKey(d)) _tmhmDisplayToHeader[d] = h;
            }
            _tmhmView = _tmhmAll.ToList();
            _tmhmViewDisplay = _tmhmView.Select(h => _tmhmHeaderToDisplay.TryGetValue(h, out var d) ? d : h).ToList();
            TmHmList.ItemsSource = _tmhmViewDisplay;
            // Preselect those that contain this species (map to display)
            var selected = Data.HGParsers.TmHmSelectedForSpecies;
            TmHmList.SelectedItems.Clear();
            foreach (var hdr in _tmhmView)
            {
                if (selected.Contains(hdr))
                {
                    var disp = _tmhmHeaderToDisplay.TryGetValue(hdr, out var dd) ? dd : hdr;
                    TmHmList.SelectedItems.Add(disp);
                }
            }

            var ov = Data.HGParsers.Overview;
            if (ov != null)
            {
                Type1Box.SelectedItem = FormatDisplay(ov.Type1);
                Type2Box.SelectedItem = FormatDisplay(ov.Type2);
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
                Ability1Box.Text = FormatDisplay(ov.Ability1);
                Ability2Box.Text = FormatDisplay(ov.Ability2);
                AbilityHiddenBox.Text = FormatDisplay(ov.AbilityHidden);
                Egg1Box.SelectedItem = FormatDisplay(ov.EggGroup1);
                Egg2Box.SelectedItem = FormatDisplay(ov.EggGroup2);
                GrowthBox.SelectedItem = FormatDisplay(ov.GrowthRate);
                GenderBox.Text = ov.GenderRatio.ToString();
                CatchBox.Text = ov.CatchRate.ToString();
                BaseExpBox.Text = ov.BaseExp.ToString();
                EggCyclesBox.Text = ov.EggCycles.ToString();
                FriendBox.Text = ov.BaseFriendship.ToString();
                Item1Box.Text = FormatDisplay(ov.Item1);
                Item2Box.Text = FormatDisplay(ov.Item2);
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
            var movePicker = new ComboBox { IsEditable = true, ItemsSource = _moveIndex.Select(FormatMove).ToList(), PlaceholderText = "Move" };
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
                var move = MapMoveDisplayToMacro((movePicker.Text ?? string.Empty).Trim());
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
            var eggPicker = new ComboBox { IsEditable = true, ItemsSource = _moveIndex.Select(FormatMove).ToList(), PlaceholderText = "Move" };
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
                var text = MapMoveDisplayToMacro((eggPicker.Text ?? string.Empty).Trim());
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
            var selectedDisplay = TmHmList.SelectedItems.Cast<string>().ToList();
            var selected = selectedDisplay.Select(d => _tmhmDisplayToHeader.TryGetValue(d, out var h) ? h : d).ToList();
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
                    // display format is "Tutor_X: Move cost"; map back to header first
                    var header = _tutorDisplayToHeader.TryGetValue(s, out var h) ? h : s;
                    var parts = header.Split(':');
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
                    var header = _tutorDisplayToHeader.TryGetValue(s, out var h) ? h : s;
                    var parts = header.Split(':');
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
            var hidden = MapAbilityToMacro(AbilityHiddenBox.Text);
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
            var ability = MapAbilityToMacro(AbilityHiddenBox.Text);
            if (string.IsNullOrWhiteSpace(ability) || ability == "ABILITY_NONE") return;
            await Data.HGSerializers.SaveHiddenAbilityAsync(_species.Name, ability);
        }

        private async void OnPreviewHiddenAbility(object sender, RoutedEventArgs e)
        {
            if (_species == null) return;
            var ability = MapAbilityToMacro(AbilityHiddenBox.Text);
            if (string.IsNullOrWhiteSpace(ability) || ability == "ABILITY_NONE") return;
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
            ov.Ability1 = MapAbilityToMacro(Ability1Box.Text);
            ov.Ability2 = MapAbilityToMacro(Ability2Box.Text);
            ov.AbilityHidden = MapAbilityToMacro(AbilityHiddenBox.Text);
            ov.EggGroup1 = (Egg1Box.SelectedItem as string) ?? "EGG_GROUP_NONE";
            ov.EggGroup2 = (Egg2Box.SelectedItem as string) ?? "EGG_GROUP_NONE";
            ov.GrowthRate = (GrowthBox.SelectedItem as string) ?? "GROWTH_MEDIUM_FAST";
            int.TryParse(GenderBox.Text, out v); ov.GenderRatio = v;
            int.TryParse(CatchBox.Text, out v); ov.CatchRate = v;
            int.TryParse(BaseExpBox.Text, out v); ov.BaseExp = v;
            int.TryParse(EggCyclesBox.Text, out v); ov.EggCycles = v;
            int.TryParse(FriendBox.Text, out v); ov.BaseFriendship = v;
            ov.Item1 = MapItemToMacro(Item1Box.Text);
            ov.Item2 = MapItemToMacro(Item2Box.Text);
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

        // Ability autosuggest handlers
        private void OnAbilityTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                if (!args.Reason.Equals(AutoSuggestionBoxTextChangeReason.UserInput)) return;
                var query = (sender.Text ?? string.Empty).Trim();
                var list = string.IsNullOrEmpty(query)
                    ? _abilityIndex
                    : _abilityIndex.Where(a => a.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
                sender.ItemsSource = list.Select(FormatDisplay).ToList();
            }
            catch { }
        }

        private void OnAbilitySuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            try
            {
                var display = args.SelectedItem as string ?? sender.Text;
                // Map back to macro by reverse formatting
                string macro = _abilityIndex.FirstOrDefault(a => string.Equals(FormatDisplay(a), display, StringComparison.OrdinalIgnoreCase))
                                ?? _abilityIndex.FirstOrDefault(a => a.Contains(display, StringComparison.OrdinalIgnoreCase))
                                ?? display;
                sender.Text = FormatDisplay(macro);
            }
            catch { }
        }

        private void OpenAbilityDropdown(AutoSuggestBox box)
        {
            try
            {
                if (box == null) return;
                // Ensure full list shown
                box.ItemsSource = _abilityIndex.Select(FormatDisplay).ToList();
                box.IsSuggestionListOpen = true;
            }
            catch { }
        }

        private void OnAbility1Dropdown(object sender, RoutedEventArgs e) => OpenAbilityDropdown(Ability1Box);
        private void OnAbility2Dropdown(object sender, RoutedEventArgs e) => OpenAbilityDropdown(Ability2Box);
        private void OnAbilityHiddenDropdown(object sender, RoutedEventArgs e) => OpenAbilityDropdown(AbilityHiddenBox);

        // Item autosuggest handlers
        private void OnItemTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                if (!args.Reason.Equals(AutoSuggestionBoxTextChangeReason.UserInput)) return;
                var query = (sender.Text ?? string.Empty).Trim();
                var list = string.IsNullOrEmpty(query)
                    ? _itemIndex
                    : _itemIndex.Where(i => i.Contains(query, StringComparison.OrdinalIgnoreCase) || FormatDisplay(i).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
                sender.ItemsSource = list.Select(FormatDisplay).ToList();
            }
            catch { }
        }

        private void OnItemSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            try
            {
                var display = args.SelectedItem as string ?? sender.Text;
                string macro = _itemIndex.FirstOrDefault(a => string.Equals(FormatDisplay(a), display, StringComparison.OrdinalIgnoreCase))
                                ?? _itemIndex.FirstOrDefault(a => a.Contains(display, StringComparison.OrdinalIgnoreCase))
                                ?? display;
                sender.Text = FormatDisplay(macro);
            }
            catch { }
        }

        private void OpenItemDropdown(AutoSuggestBox box)
        {
            try
            {
                if (box == null) return;
                box.ItemsSource = _itemIndex.Select(FormatDisplay).ToList();
                box.IsSuggestionListOpen = true;
            }
            catch { }
        }

        private void OnItem1Dropdown(object sender, RoutedEventArgs e) => OpenItemDropdown(Item1Box);
        private void OnItem2Dropdown(object sender, RoutedEventArgs e) => OpenItemDropdown(Item2Box);

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
                if (level < 0 || level > 100) errors.Add($"Row {i + 1}: Level must be 0-100.");
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
            _tmhmView = string.IsNullOrEmpty(q)
                ? _tmhmAll.ToList()
                : _tmhmAll.Where(x => (_tmhmHeaderToDisplay.TryGetValue(x, out var d) ? d : x).Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            _tmhmViewDisplay = _tmhmView.Select(h => _tmhmHeaderToDisplay.TryGetValue(h, out var d) ? d : h).ToList();
            TmHmList.ItemsSource = _tmhmViewDisplay;
        }

        private void OnTutorSearch(object sender, AutoSuggestBoxTextChangedEventArgs e)
        {
            var q = (TutorSearch.Text ?? string.Empty).Trim();
            _tutorView = string.IsNullOrEmpty(q) ? _tutorAll.ToList() : _tutorAll.Where(x => ($"{FormatDisplay(x.tutor)} {FormatMove(x.move)} {x.cost}").Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            _tutorViewDisplay = _tutorView.Select(t => $"{FormatDisplay(t.tutor)}: {FormatMove(t.move)} {t.cost}").ToList();
            TutorList.ItemsSource = _tutorViewDisplay;
        }
    }
}


