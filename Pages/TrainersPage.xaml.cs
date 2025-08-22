using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HGEngineGUI.Pages
{
    public sealed partial class TrainersPage : Page
    {
        private List<string> _all = new();
        private List<string> _filtered = new();
        private List<PartyRow> _partyRows = new();
        public List<string> ItemMacroList { get; private set; } = new();
        public List<string> MoveMacroList { get; private set; } = new();
        public List<string> SpeciesMacroList { get; private set; } = new();
        private int _currentTrainerId = -1;
        private string _currentTrainerName = string.Empty;

        public TrainersPage()
        {
            InitializeComponent();
            LoadAsync();
        }

        private async void LoadAsync()
        {
            if (ProjectContext.RootPath == null)
            {
                // Nothing to parse yet; prompt user to pick the project folder first
                try
                {
                    var dlg = new ContentDialog
                    {
                        Title = "No project selected",
                        Content = "Open the Project tab and pick your HG Engine folder before using Trainers.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dlg.ShowAsync();
                }
                catch { }
                return;
            }
            try
            {
                await Data.HGParsers.RefreshTrainersAsync();
                _all = Data.HGParsers.Trainers.Select(t => $"{t.Id}: {t.Name} [{t.Class}] mons={t.NumMons} AI={t.AIFlags} Type={t.BattleType} Items={string.Join(",", t.Items)}").ToList();
                ApplyFilter();

                // Fill dropdowns
                ClassCombo.ItemsSource = Data.HGParsers.TrainerClasses.Select(tc => tc.Name).ToList();
                ItemMacroList = new List<string> { "ITEM_NONE" };
                ItemMacroList.AddRange(Data.HGParsers.ItemMacros);
                Item1Combo.ItemsSource = ItemMacroList;
                Item2Combo.ItemsSource = ItemMacroList;
                Item3Combo.ItemsSource = ItemMacroList;
                Item4Combo.ItemsSource = ItemMacroList;

                MoveMacroList = Data.HGParsers.MoveMacros.ToList();
                SpeciesMacroList = Data.HGParsers.SpeciesMacroNames.ToList();
            }
            catch (Exception ex)
            {
                try
                {
                    var dlg = new ContentDialog
                    {
                        Title = "Failed to load trainers",
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dlg.ShowAsync();
                }
                catch { }
            }
        }

        private void ApplyFilter()
        {
            var term = SearchBox?.Text?.Trim() ?? string.Empty;
            _filtered = string.IsNullOrEmpty(term) ? _all : _all.Where(x => x.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            TrainerList.ItemsSource = _filtered;
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private async void OnTrainerClick(object sender, ItemClickEventArgs e)
        {
            var text = e.ClickedItem as string;
            if (string.IsNullOrEmpty(text)) return;
            var idStr = text.Split(':')[0];
            if (int.TryParse(idStr, out var id))
            {
                try
                {
                    _currentTrainerId = id;
                    await Data.HGParsers.RefreshTrainerDetailsAsync(id);
                    var header = Data.HGParsers.Trainers.FirstOrDefault(t => t.Id == id);
                    _currentTrainerName = header?.Name ?? string.Empty;
                    HeaderText.Text = header != null ? $"{header.Id}: {header.Name} [{header.Class}] • mons={header.NumMons} • AI={header.AIFlags} • Type={header.BattleType}" : $"Trainer {id}";
                    _partyRows = Data.HGParsers.CurrentTrainerParty.Select(m => new PartyRow
                    {
                        Slot = m.Index,
                        Species = m.Species,
                        Level = m.Level,
                        IVs = m.IVs,
                        AbilitySlot = m.AbilitySlot,
                        Item = m.Item,
                        Move1 = m.Move1,
                        Move2 = m.Move2,
                        Move3 = m.Move3,
                        Move4 = m.Move4,
                        Nature = m.Nature,
                        Form = m.Form,
                        Ball = m.Ball,
                        ShinyLock = m.ShinyLock,
                        Nickname = m.Nickname,
                        PP = m.PP
                    }).ToList();
                    RenderPartyStack();

                    if (header != null)
                    {
                        ClassCombo.SelectedItem = header.Class;
                        AIFlagsBox.Text = header.AIFlags;
                        BattleTypeCombo.SelectedIndex = header.BattleType == "DOUBLE_BATTLE" ? 1 : 0;
                        Item1Combo.SelectedItem = header.Items.ElementAtOrDefault(0) ?? "ITEM_NONE";
                        Item2Combo.SelectedItem = header.Items.ElementAtOrDefault(1) ?? "ITEM_NONE";
                        Item3Combo.SelectedItem = header.Items.ElementAtOrDefault(2) ?? "ITEM_NONE";
                        Item4Combo.SelectedItem = header.Items.ElementAtOrDefault(3) ?? "ITEM_NONE";
                    }
                    UpdateHeaderSummary();
                }
                catch (Exception ex)
                {
                    try
                    {
                        var dlg = new ContentDialog
                        {
                            Title = "Failed to load trainer",
                            Content = ex.Message,
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await dlg.ShowAsync();
                    }
                    catch { }
                }
            }
        }

        private async void OnSaveParty(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (TrainerList.SelectedItem is string s && int.TryParse(s.Split(':')[0], out var id))
            {
                SyncPartyFromUi();
                // Validate before saving
                var errors = ValidateParty(_partyRows);
                if (errors.Count > 0)
                {
                    var dlg = new ContentDialog { Title = "Fix validation errors", Content = string.Join("\n", errors), PrimaryButtonText = "Close", XamlRoot = this.XamlRoot };
                    await dlg.ShowAsync();
                    return;
                }
                // Save party
                await Data.HGSerializers.SaveTrainerPartyAsync(id, _partyRows);
                await LoadUpdatedAsync();
            }
        }

        private void RenderPartyStack()
        {
            if (PartyStack == null) return;
            PartyStack.Children.Clear();
            foreach (var row in _partyRows.OrderBy(r => r.Slot))
            {
                var container = new StackPanel { Spacing = 8, Padding = new Microsoft.UI.Xaml.Thickness(4) };

                var topGrid = new Grid { ColumnSpacing = 12 };
                topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Microsoft.UI.Xaml.GridLength(1, Microsoft.UI.Xaml.GridUnitType.Auto) });
                for (int i = 0; i < 7; i++) topGrid.ColumnDefinitions.Add(new ColumnDefinition());

                var slotStack = new StackPanel { Width = 72 };
                slotStack.Children.Add(new TextBlock { Text = "Slot", FontSize = 12 });
                var slotBox = new TextBox { Width = 72, Text = row.Slot.ToString() };
                slotBox.TextChanging += (s, e) => { if (int.TryParse(slotBox.Text, out var v)) row.Slot = v; };
                slotStack.Children.Add(slotBox);
                topGrid.Children.Add(slotStack);
                Grid.SetColumn(slotStack, 0);

                var speciesStack = new StackPanel { Width = 220 };
                speciesStack.Children.Add(new TextBlock { Text = "Species", FontSize = 12 });
                var speciesCombo = new ComboBox { IsEditable = true, ItemsSource = SpeciesMacroList, SelectedItem = row.Species };
                speciesCombo.SelectionChanged += (s, e) => { row.Species = (speciesCombo.SelectedItem as string) ?? (speciesCombo.Text ?? string.Empty); };
                speciesCombo.LostFocus += (s, e) => { row.Species = speciesCombo.Text ?? string.Empty; };
                speciesStack.Children.Add(speciesCombo);
                topGrid.Children.Add(speciesStack);
                Grid.SetColumn(speciesStack, 1);

                var levelStack = new StackPanel { Width = 100 };
                levelStack.Children.Add(new TextBlock { Text = "Level", FontSize = 12 });
                var levelBox = new TextBox { Text = row.Level.ToString() };
                levelBox.TextChanging += (s, e) => { if (int.TryParse(levelBox.Text, out var v)) row.Level = v; };
                levelStack.Children.Add(levelBox);
                topGrid.Children.Add(levelStack);
                Grid.SetColumn(levelStack, 2);

                var ivsStack = new StackPanel { Width = 100 };
                ivsStack.Children.Add(new TextBlock { Text = "IVs", FontSize = 12 });
                var ivsBox = new TextBox { Text = row.IVs.ToString() };
                ivsBox.TextChanging += (s, e) => { if (int.TryParse(ivsBox.Text, out var v)) row.IVs = v; };
                ivsStack.Children.Add(ivsBox);
                topGrid.Children.Add(ivsStack);
                Grid.SetColumn(ivsStack, 3);

                var abilitySlotStack = new StackPanel { Width = 120 };
                abilitySlotStack.Children.Add(new TextBlock { Text = "AbilitySlot", FontSize = 12 });
                var abilitySlotBox = new TextBox { Text = row.AbilitySlot.ToString() };
                abilitySlotBox.TextChanging += (s, e) => { if (int.TryParse(abilitySlotBox.Text, out var v)) row.AbilitySlot = v; };
                abilitySlotStack.Children.Add(abilitySlotBox);
                topGrid.Children.Add(abilitySlotStack);
                Grid.SetColumn(abilitySlotStack, 4);

                var itemStack = new StackPanel { Width = 240 };
                itemStack.Children.Add(new TextBlock { Text = "Item", FontSize = 12 });
                var itemCombo = new ComboBox { IsEditable = true, ItemsSource = ItemMacroList, SelectedItem = row.Item };
                itemCombo.SelectionChanged += (s, e) => { row.Item = (itemCombo.SelectedItem as string) ?? (itemCombo.Text ?? string.Empty); };
                itemCombo.LostFocus += (s, e) => { row.Item = itemCombo.Text ?? string.Empty; };
                itemStack.Children.Add(itemCombo);
                topGrid.Children.Add(itemStack);
                Grid.SetColumn(itemStack, 5);

                container.Children.Add(new TextBlock { Text = "Basics", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 0) });
                container.Children.Add(topGrid);

                var moveGrid = new Grid { ColumnSpacing = 8 };
                for (int i = 0; i < 4; i++) moveGrid.ColumnDefinitions.Add(new ColumnDefinition());
                container.Children.Add(new TextBlock { Text = "Moves", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0) });
                container.Children.Add(moveGrid);

                void AddMove(StackPanel parent, string label, Func<string> getVal, Action<string> setVal)
                {
                    parent.Children.Add(new TextBlock { Text = label, FontSize = 12 });
                    var initial = getVal() ?? string.Empty;
                    var cb = new ComboBox { IsEditable = true, ItemsSource = MoveMacroList, SelectedItem = initial };
                    cb.Text = initial;
                    cb.SelectionChanged += (s, e) => setVal((cb.SelectedItem as string) ?? (cb.Text ?? string.Empty));
                    cb.LostFocus += (s, e) => setVal(cb.Text ?? string.Empty);
                    parent.Children.Add(cb);
                }

                var m1 = new StackPanel(); AddMove(m1, "Move 1", () => row.Move1, v => row.Move1 = v); moveGrid.Children.Add(m1); Grid.SetColumn(m1, 0);
                var m2 = new StackPanel(); AddMove(m2, "Move 2", () => row.Move2, v => row.Move2 = v); moveGrid.Children.Add(m2); Grid.SetColumn(m2, 1);
                var m3 = new StackPanel(); AddMove(m3, "Move 3", () => row.Move3, v => row.Move3 = v); moveGrid.Children.Add(m3); Grid.SetColumn(m3, 2);
                var m4 = new StackPanel(); AddMove(m4, "Move 4", () => row.Move4, v => row.Move4 = v); moveGrid.Children.Add(m4); Grid.SetColumn(m4, 3);

                var miscGrid = new Grid { ColumnSpacing = 8 };
                for (int i = 0; i < 4; i++) miscGrid.ColumnDefinitions.Add(new ColumnDefinition());
                var natureStack = new StackPanel(); natureStack.Children.Add(new TextBlock { Text = "Nature", FontSize = 12 }); var natureBox = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.NatureMacros, SelectedItem = row.Nature }; natureBox.SelectionChanged += (s, e) => { row.Nature = (natureBox.SelectedItem as string) ?? (natureBox.Text ?? string.Empty); }; natureBox.LostFocus += (s, e) => { row.Nature = natureBox.Text ?? string.Empty; }; natureStack.Children.Add(natureBox); miscGrid.Children.Add(natureStack); Grid.SetColumn(natureStack, 0);
                var formStack = new StackPanel(); formStack.Children.Add(new TextBlock { Text = "Form", FontSize = 12 }); var formBox = new TextBox { Text = row.Form.ToString() }; formBox.TextChanging += (s, e) => { if (int.TryParse(formBox.Text, out var v)) row.Form = v; }; formStack.Children.Add(formBox); miscGrid.Children.Add(formStack); Grid.SetColumn(formStack, 1);
                var ballStack = new StackPanel(); ballStack.Children.Add(new TextBlock { Text = "Ball", FontSize = 12 }); var ballBox = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.BallMacros, SelectedItem = row.Ball }; ballBox.SelectionChanged += (s, e) => { row.Ball = (ballBox.SelectedItem as string) ?? (ballBox.Text ?? string.Empty); }; ballBox.LostFocus += (s, e) => { row.Ball = ballBox.Text ?? string.Empty; }; ballStack.Children.Add(ballBox); miscGrid.Children.Add(ballStack); Grid.SetColumn(ballStack, 2);
                var shinyStack = new StackPanel(); shinyStack.Children.Add(new TextBlock { Text = "Shiny Lock", FontSize = 12 }); var shinyBox = new CheckBox { IsChecked = row.ShinyLock }; shinyBox.Checked += (s, e) => row.ShinyLock = true; shinyBox.Unchecked += (s, e) => row.ShinyLock = false; shinyStack.Children.Add(shinyBox); miscGrid.Children.Add(shinyStack); Grid.SetColumn(shinyStack, 3);
                container.Children.Add(new TextBlock { Text = "Details", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0) });
                container.Children.Add(miscGrid);

                // Collapsible Advanced section per mon
                var expander = new Expander { Header = "Advanced", IsExpanded = false };
                var adv = new StackPanel { Spacing = 6 };
                var advGrid1 = new Grid { ColumnSpacing = 8 };
                for (int i = 0; i < 4; i++) advGrid1.ColumnDefinitions.Add(new ColumnDefinition());
                var advAbilityStack = new StackPanel(); advAbilityStack.Children.Add(new TextBlock { Text = "Ability (macro)", FontSize = 12 }); var advAbilityBox = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.AbilityMacros, SelectedItem = row.Ability }; advAbilityBox.SelectionChanged += (s, e) => row.Ability = (advAbilityBox.SelectedItem as string) ?? (advAbilityBox.Text ?? string.Empty); advAbilityBox.LostFocus += (s, e) => row.Ability = advAbilityBox.Text ?? string.Empty; advAbilityStack.Children.Add(advAbilityBox); advGrid1.Children.Add(advAbilityStack); Grid.SetColumn(advAbilityStack, 0);
                var ballsealStack = new StackPanel(); ballsealStack.Children.Add(new TextBlock { Text = "BallSeal", FontSize = 12 }); var ballsealBox = new TextBox { Text = row.BallSeal.ToString() }; ballsealBox.TextChanging += (s, e) => { if (int.TryParse(ballsealBox.Text, out var v)) row.BallSeal = v; }; ballsealStack.Children.Add(ballsealBox); advGrid1.Children.Add(ballsealStack); Grid.SetColumn(ballsealStack, 1);
                var typesStack = new StackPanel(); typesStack.Children.Add(new TextBlock { Text = "Types (TYPE_*, TYPE_*)", FontSize = 12 }); var typesBox1 = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.TypeMacros, SelectedItem = row.Type1 }; typesBox1.SelectionChanged += (s, e) => row.Type1 = (typesBox1.SelectedItem as string) ?? (typesBox1.Text ?? string.Empty); typesBox1.LostFocus += (s, e) => row.Type1 = typesBox1.Text ?? string.Empty; typesStack.Children.Add(typesBox1); var typesBox2 = new ComboBox { IsEditable = true, ItemsSource = Data.HGParsers.TypeMacros, SelectedItem = row.Type2 }; typesBox2.SelectionChanged += (s, e) => row.Type2 = (typesBox2.SelectedItem as string) ?? (typesBox2.Text ?? string.Empty); typesBox2.LostFocus += (s, e) => row.Type2 = typesBox2.Text ?? string.Empty; typesStack.Children.Add(typesBox2); advGrid1.Children.Add(typesStack); Grid.SetColumn(typesStack, 2);
                var flagsStack = new StackPanel(); flagsStack.Children.Add(new TextBlock { Text = "AdditionalFlags", FontSize = 12 }); var flagsBox = new TextBox { Text = row.AdditionalFlags.ToString() }; flagsBox.TextChanging += (s, e) => { if (int.TryParse(flagsBox.Text, out var v)) row.AdditionalFlags = v; }; flagsStack.Children.Add(flagsBox); advGrid1.Children.Add(flagsStack); Grid.SetColumn(flagsStack, 3);
                adv.Children.Add(advGrid1);

                var advGrid2 = new Grid { ColumnSpacing = 8 };
                for (int i = 0; i < 6; i++) advGrid2.ColumnDefinitions.Add(new ColumnDefinition());
                StackPanel Stat(string label, Func<int> get, Action<int> set)
                {
                    var sp = new StackPanel(); sp.Children.Add(new TextBlock { Text = label, FontSize = 12 }); var tb = new TextBox { Text = get().ToString() }; tb.TextChanging += (s, e) => { if (int.TryParse(tb.Text, out var v)) set(v); }; sp.Children.Add(tb); return sp;
                }
                var statHp = Stat("HP", () => row.Hp, v => row.Hp = v); advGrid2.Children.Add(statHp); Grid.SetColumn(statHp, 0);
                var statAtk = Stat("Atk", () => row.Atk, v => row.Atk = v); advGrid2.Children.Add(statAtk); Grid.SetColumn(statAtk, 1);
                var statDef = Stat("Def", () => row.Def, v => row.Def = v); advGrid2.Children.Add(statDef); Grid.SetColumn(statDef, 2);
                var statSpe = Stat("Spe", () => row.Speed, v => row.Speed = v); advGrid2.Children.Add(statSpe); Grid.SetColumn(statSpe, 3);
                var statSpA = Stat("SpAtk", () => row.SpAtk, v => row.SpAtk = v); advGrid2.Children.Add(statSpA); Grid.SetColumn(statSpA, 4);
                var statSpD = Stat("SpDef", () => row.SpDef, v => row.SpDef = v); advGrid2.Children.Add(statSpD); Grid.SetColumn(statSpD, 5);
                adv.Children.Add(advGrid2);

                var advGrid3 = new Grid { ColumnSpacing = 8 };
                for (int i = 0; i < 3; i++) advGrid3.ColumnDefinitions.Add(new ColumnDefinition());
                var ivnumsStack = new StackPanel(); ivnumsStack.Children.Add(new TextBlock { Text = "IVNums (6 comma)", FontSize = 12 }); var ivnumsBox = new TextBox { Text = row.IVNums }; ivnumsBox.TextChanging += (s, e) => row.IVNums = ivnumsBox.Text; ivnumsStack.Children.Add(ivnumsBox); advGrid3.Children.Add(ivnumsStack); Grid.SetColumn(ivnumsStack, 0);
                var evnumsStack = new StackPanel(); evnumsStack.Children.Add(new TextBlock { Text = "EVNums (6 comma)", FontSize = 12 }); var evnumsBox = new TextBox { Text = row.EVNums }; evnumsBox.TextChanging += (s, e) => row.EVNums = evnumsBox.Text; evnumsStack.Children.Add(evnumsBox); advGrid3.Children.Add(evnumsStack); Grid.SetColumn(evnumsStack, 1);
                var statusStack = new StackPanel(); statusStack.Children.Add(new TextBlock { Text = "Status", FontSize = 12 }); var statusBox = new TextBox { Text = row.Status.ToString() }; statusBox.TextChanging += (s, e) => { if (int.TryParse(statusBox.Text, out var v)) row.Status = v; }; statusStack.Children.Add(statusBox); advGrid3.Children.Add(statusStack); Grid.SetColumn(statusStack, 2);
                adv.Children.Add(advGrid3);

                var ppCountsStack = new StackPanel(); ppCountsStack.Children.Add(new TextBlock { Text = "PP Counts (4 comma)", FontSize = 12 }); var ppCountsBox = new TextBox { Text = row.PPCounts }; ppCountsBox.TextChanging += (s, e) => row.PPCounts = ppCountsBox.Text; ppCountsStack.Children.Add(ppCountsBox); adv.Children.Add(ppCountsStack);

                // Move PP and Nickname into Advanced section
                var advGrid4 = new Grid { ColumnSpacing = 8 };
                advGrid4.ColumnDefinitions.Add(new ColumnDefinition());
                advGrid4.ColumnDefinitions.Add(new ColumnDefinition());
                var ppStack = new StackPanel(); ppStack.Children.Add(new TextBlock { Text = "PP (comma-separated)", FontSize = 12 }); var ppBox = new TextBox { Text = row.PP }; ppBox.TextChanging += (s, e) => row.PP = ppBox.Text; ppStack.Children.Add(ppBox); advGrid4.Children.Add(ppStack); Grid.SetColumn(ppStack, 0);
                var nickStack = new StackPanel(); nickStack.Children.Add(new TextBlock { Text = "Nickname", FontSize = 12 }); var nickBox = new TextBox { Text = row.Nickname }; nickBox.TextChanging += (s, e) => row.Nickname = nickBox.Text; nickStack.Children.Add(nickBox); advGrid4.Children.Add(nickStack); Grid.SetColumn(nickStack, 1);
                adv.Children.Add(advGrid4);

                expander.Content = adv;
                container.Children.Add(expander);

                // Visual separation for each party mon
                var border = new Microsoft.UI.Xaml.Controls.Border
                {
                    BorderThickness = new Microsoft.UI.Xaml.Thickness(1),
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 6, 0, 6),
                    Padding = new Microsoft.UI.Xaml.Thickness(6),
                    Child = container
                };

                PartyStack.Children.Add(border);
            }
            UpdateHeaderSummary();
        }

        private void SyncPartyFromUi()
        {
            _partyRows = _partyRows.Select(r => new PartyRow
            {
                Slot = r.Slot,
                Species = (r.Species ?? string.Empty).Trim(),
                Level = r.Level,
                IVs = r.IVs,
                AbilitySlot = r.AbilitySlot,
                Item = (r.Item ?? string.Empty).Trim(),
                Move1 = (r.Move1 ?? string.Empty).Trim(),
                Move2 = (r.Move2 ?? string.Empty).Trim(),
                Move3 = (r.Move3 ?? string.Empty).Trim(),
                Move4 = (r.Move4 ?? string.Empty).Trim(),
                Nature = (r.Nature ?? string.Empty).Trim(),
                Form = r.Form,
                Ball = (r.Ball ?? string.Empty).Trim(),
                ShinyLock = r.ShinyLock,
                Nickname = r.Nickname ?? string.Empty,
                PP = r.PP ?? string.Empty,
                Ability = r.Ability ?? string.Empty,
                BallSeal = r.BallSeal,
                IVNums = r.IVNums ?? string.Empty,
                EVNums = r.EVNums ?? string.Empty,
                Status = r.Status,
                Hp = r.Hp,
                Atk = r.Atk,
                Def = r.Def,
                Speed = r.Speed,
                SpAtk = r.SpAtk,
                SpDef = r.SpDef,
                Type1 = r.Type1 ?? string.Empty,
                Type2 = r.Type2 ?? string.Empty,
                PPCounts = r.PPCounts ?? string.Empty,
                AdditionalFlags = r.AdditionalFlags
            }).ToList();
        }

        // QoL actions for party rows (operate on the last focused row; fallback to first)
        private PartyRow? GetActivePartyRow() => _partyRows.OrderBy(r => r.Slot).FirstOrDefault();

        private void OnClonePartyRow(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var row = GetActivePartyRow(); if (row == null) return;
            var clone = new PartyRow
            {
                Slot = row.Slot + 1,
                Species = row.Species,
                Level = row.Level,
                IVs = row.IVs,
                AbilitySlot = row.AbilitySlot,
                Item = row.Item,
                Move1 = row.Move1,
                Move2 = row.Move2,
                Move3 = row.Move3,
                Move4 = row.Move4,
                Nature = row.Nature,
                Form = row.Form,
                Ball = row.Ball,
                ShinyLock = row.ShinyLock,
                Nickname = row.Nickname,
                PP = row.PP
            };
            _partyRows.Add(clone);
            RenderPartyStack();
            UpdateHeaderSummary();
        }

        private void OnMovePartyRowUp(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var row = GetActivePartyRow(); if (row == null) return;
            row.Slot = Math.Max(0, row.Slot - 1);
            RenderPartyStack();
        }

        private void OnMovePartyRowDown(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var row = GetActivePartyRow(); if (row == null) return;
            row.Slot = row.Slot + 1;
            RenderPartyStack();
        }

        private void OnDeletePartyRow(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var row = GetActivePartyRow(); if (row == null) return;
            _partyRows.Remove(row);
            RenderPartyStack();
            UpdateHeaderSummary();
        }

        private void UpdateHeaderSummary()
        {
            if (_currentTrainerId < 0) return;
            var cls = ClassCombo.SelectedItem as string ?? string.Empty;
            var ai = AIFlagsBox.Text ?? string.Empty;
            var bt = (BattleTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SINGLE_BATTLE";
            var name = string.IsNullOrWhiteSpace(_currentTrainerName) ? "" : _currentTrainerName;
            HeaderText.Text = $"{_currentTrainerId}: {name} [{cls}] • mons={_partyRows.Count} • AI={ai} • Type={bt}";
        }

        

        private async void OnOpenTrainersFile(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (ProjectContext.RootPath == null) return;
            var path = System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            if (!System.IO.File.Exists(path)) return;
            try { await Windows.System.Launcher.LaunchUriAsync(new Uri($"file:///{path.Replace("\\", "/")}")); } catch { }
        }

        private List<string> ValidateParty(List<PartyRow> rows)
        {
            var errors = new List<string>();
            var species = new HashSet<string>(Data.HGParsers.SpeciesMacroNames);
            var moves = new HashSet<string>(Data.HGParsers.MoveMacros);
            var items = new HashSet<string>(Data.HGParsers.ItemMacros.Concat(new[] { "ITEM_NONE" }));
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r.Level < 1 || r.Level > 100) errors.Add($"Row {i}: Level must be 1-100");
                if (r.IVs < 0 || r.IVs > 31) errors.Add($"Row {i}: IVs must be 0-31");
                if (r.AbilitySlot < 0 || r.AbilitySlot > 2) errors.Add($"Row {i}: AbilitySlot must be 0-2");
                if (!species.Contains(r.Species)) errors.Add($"Row {i}: Unknown species");
                if (!string.IsNullOrWhiteSpace(r.Item) && !items.Contains(r.Item)) errors.Add($"Row {i}: Unknown item");
                if (!string.IsNullOrWhiteSpace(r.Move1) && !moves.Contains(r.Move1)) errors.Add($"Row {i}: Move1 invalid");
                if (!string.IsNullOrWhiteSpace(r.Move2) && !moves.Contains(r.Move2)) errors.Add($"Row {i}: Move2 invalid");
                if (!string.IsNullOrWhiteSpace(r.Move3) && !moves.Contains(r.Move3)) errors.Add($"Row {i}: Move3 invalid");
                if (!string.IsNullOrWhiteSpace(r.Move4) && !moves.Contains(r.Move4)) errors.Add($"Row {i}: Move4 invalid");
            }
            return errors;
        }

        private async void OnPreviewParty(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (TrainerList.SelectedItem is not string s || !int.TryParse(s.Split(':')[0], out var id)) return;
            if (ProjectContext.RootPath == null) return;
            var path = System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            if (!System.IO.File.Exists(path)) return;

            var original = await System.IO.File.ReadAllTextAsync(path);
            // Generate updated text for party region only using the same logic as save
            var partyRegex = new System.Text.RegularExpressions.Regex($"party\\s+{id}\\s*(?<body>[\\s\\S]*?)endparty", System.Text.RegularExpressions.RegexOptions.Multiline);
            var m = partyRegex.Match(original);
            if (!m.Success) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"party {id}");
            foreach (var row in _partyRows.OrderBy(r => r.Slot))
            {
                sb.AppendLine($"        // mon {row.Slot}");
                sb.AppendLine($"        ivs {row.IVs}");
                sb.AppendLine($"        abilityslot {row.AbilitySlot}");
                sb.AppendLine($"        level {row.Level}");
                sb.AppendLine($"        pokemon {row.Species}");
                if (!string.IsNullOrEmpty(row.Item)) sb.AppendLine($"        item {row.Item}");
                if (!string.IsNullOrEmpty(row.Move1)) sb.AppendLine($"        move {row.Move1}");
                if (!string.IsNullOrEmpty(row.Move2)) sb.AppendLine($"        move {row.Move2}");
                if (!string.IsNullOrEmpty(row.Move3)) sb.AppendLine($"        move {row.Move3}");
                if (!string.IsNullOrEmpty(row.Move4)) sb.AppendLine($"        move {row.Move4}");
                sb.AppendLine($"        ballseal 0");
                sb.AppendLine();
            }
            sb.AppendLine("    endparty");

            var updated = original.Substring(0, m.Index) + sb.ToString() + original.Substring(m.Index + m.Length);
            var diff = Data.HGSerializers.ComputeUnifiedDiff(original, updated, $"trainers.s (party {id})");

            var dlg = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Preview changes",
                PrimaryButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Content = new Microsoft.UI.Xaml.Controls.ScrollViewer
                {
                    HorizontalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto,
                    Content = new Microsoft.UI.Xaml.Controls.TextBox
                    {
                        Text = diff,
                        IsReadOnly = true,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                    }
                }
            };
            await dlg.ShowAsync();
        }

        public class PartyRow
        {
            public int Slot { get; set; }
            public string Species { get; set; } = string.Empty;
            public int Level { get; set; }
            public int IVs { get; set; }
            public int AbilitySlot { get; set; }
            public string Item { get; set; } = "ITEM_NONE";
            public string Move1 { get; set; } = "MOVE_NONE";
            public string Move2 { get; set; } = "MOVE_NONE";
            public string Move3 { get; set; } = "MOVE_NONE";
            public string Move4 { get; set; } = "MOVE_NONE";
            public string Nature { get; set; } = string.Empty; // placeholder, not yet parsed
            public int Form { get; set; } // placeholder
            public string Ball { get; set; } = string.Empty; // placeholder
            public bool ShinyLock { get; set; } // placeholder
            public string Nickname { get; set; } = string.Empty; // placeholder
            public string PP { get; set; } = string.Empty; // placeholder

            // Advanced
            public string Ability { get; set; } = string.Empty;
            public int BallSeal { get; set; }
            public string IVNums { get; set; } = string.Empty;
            public string EVNums { get; set; } = string.Empty;
            public int Status { get; set; }
            public int Hp { get; set; }
            public int Atk { get; set; }
            public int Def { get; set; }
            public int Speed { get; set; }
            public int SpAtk { get; set; }
            public int SpDef { get; set; }
            public string Type1 { get; set; } = string.Empty;
            public string Type2 { get; set; } = string.Empty;
            public string PPCounts { get; set; } = string.Empty;
            public int AdditionalFlags { get; set; }
        }

        private async void OnSaveTrainer(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (TrainerList.SelectedItem is string s && int.TryParse(s.Split(':')[0], out var id))
            {
                var cls = ClassCombo.SelectedItem as string ?? "";
                var ai = AIFlagsBox.Text ?? "";
                var bt = (BattleTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SINGLE_BATTLE";
                var items = new List<string>
                {
                    Item1Combo.SelectedItem as string ?? "ITEM_NONE",
                    Item2Combo.SelectedItem as string ?? "ITEM_NONE",
                    Item3Combo.SelectedItem as string ?? "ITEM_NONE",
                    Item4Combo.SelectedItem as string ?? "ITEM_NONE",
                };
                var dataTypeFlags = ComposeTrainerDataTypeFlags();
                await Data.HGSerializers.SaveTrainerHeaderAsync(id, cls, ai, bt, items, dataTypeFlags);
                await LoadUpdatedAsync();
            }
        }

        private async void OnPreviewTrainerHeader(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (TrainerList.SelectedItem is not string s || !int.TryParse(s.Split(':')[0], out var id)) return;
            if (ProjectContext.RootPath == null) return;
            var path = System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            if (!System.IO.File.Exists(path)) return;

            var original = await System.IO.File.ReadAllTextAsync(path);
            var headerRegex = new System.Text.RegularExpressions.Regex($"trainerdata\\s+{id},\\s*\"(?<name>[^\"]*)\"(?<body>[\\s\\S]*?)endentry", System.Text.RegularExpressions.RegexOptions.Multiline);
            var m = headerRegex.Match(original);
            if (!m.Success) return;

            // Compose new header block body
            var nummonsMatch = System.Text.RegularExpressions.Regex.Match(m.Groups["body"].Value, @"nummons\s+(?<v>\d+)");
            var nummons = nummonsMatch.Success ? nummonsMatch.Groups["v"].Value : "0";
            var sb = new System.Text.StringBuilder();
            var dataTypeFlags = ComposeTrainerDataTypeFlags();
            sb.AppendLine($"\n    trainermontype {dataTypeFlags}");
            sb.AppendLine($"    trainerclass {ClassCombo.SelectedItem as string ?? ""}");
            sb.AppendLine($"    nummons {nummons}");
            sb.AppendLine($"    item {Item1Combo.SelectedItem as string ?? "ITEM_NONE"}");
            sb.AppendLine($"    item {Item2Combo.SelectedItem as string ?? "ITEM_NONE"}");
            sb.AppendLine($"    item {Item3Combo.SelectedItem as string ?? "ITEM_NONE"}");
            sb.AppendLine($"    item {Item4Combo.SelectedItem as string ?? "ITEM_NONE"}");
            sb.AppendLine($"    aiflags {AIFlagsBox.Text ?? ""}");
            var bt = (BattleTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SINGLE_BATTLE";
            sb.AppendLine($"    battletype {bt}");
            sb.Append("    endentry\n\n");

            // Keep header line; just replace body
            int start = m.Index;
            int end = m.Index + m.Length;
            var headerLineEnd = original.IndexOf('\n', start);
            if (headerLineEnd < 0) headerLineEnd = start;
            string headerLine = original.Substring(start, headerLineEnd - start);
            string updated = original.Substring(0, start) + headerLine + sb.ToString() + original.Substring(end);
            var diff = Data.HGSerializers.ComputeUnifiedDiff(original, updated, $"trainers.s (header {id})");

            var dlg = new ContentDialog
            {
                Title = "Preview header changes",
                PrimaryButtonText = "Close",
                XamlRoot = this.XamlRoot,
                Content = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new TextBox
                    {
                        Text = diff,
                        IsReadOnly = true,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.NoWrap,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                    }
                }
            };
            await dlg.ShowAsync();
        }

        private string ComposeTrainerDataTypeFlags()
        {
            var flags = new List<string>();
            // Items
            if ((Item1Combo.SelectedItem as string) != "ITEM_NONE" || (Item2Combo.SelectedItem as string) != "ITEM_NONE" || (Item3Combo.SelectedItem as string) != "ITEM_NONE" || (Item4Combo.SelectedItem as string) != "ITEM_NONE")
                flags.Add("TRAINER_DATA_TYPE_ITEMS");
            // Moves
            bool anyMoves = _partyRows.Any(r => !string.IsNullOrWhiteSpace(r.Move1) && r.Move1 != "MOVE_NONE" || !string.IsNullOrWhiteSpace(r.Move2) && r.Move2 != "MOVE_NONE" || !string.IsNullOrWhiteSpace(r.Move3) && r.Move3 != "MOVE_NONE" || !string.IsNullOrWhiteSpace(r.Move4) && r.Move4 != "MOVE_NONE");
            if (anyMoves) flags.Add("TRAINER_DATA_TYPE_MOVES");
            // Ability macro present
            if (_partyRows.Any(r => !string.IsNullOrWhiteSpace(r.Ability))) flags.Add("TRAINER_DATA_TYPE_ABILITY");
            // Ball macro present
            if (_partyRows.Any(r => !string.IsNullOrWhiteSpace(r.Ball))) flags.Add("TRAINER_DATA_TYPE_BALL");
            // Nature
            if (_partyRows.Any(r => !string.IsNullOrWhiteSpace(r.Nature))) flags.Add("TRAINER_DATA_TYPE_NATURE_SET");
            // Shiny Lock
            if (_partyRows.Any(r => r.ShinyLock)) flags.Add("TRAINER_DATA_TYPE_SHINY_LOCK");
            // IV/EV set (includes IVNums/EVNums and type overrides per wiki)
            bool anyIvEv = _partyRows.Any(r =>
                (!string.IsNullOrWhiteSpace(r.IVNums)) || (!string.IsNullOrWhiteSpace(r.EVNums)) ||
                !string.IsNullOrWhiteSpace(r.Type1) || !string.IsNullOrWhiteSpace(r.Type2));
            if (anyIvEv) flags.Add("TRAINER_DATA_TYPE_IV_EV_SET");
            // Additional flags block (HP/Atk/Def/Spe/SpAtk/SpDef, Status, PPCounts, Nickname, BallSeal per-mon)
            bool anyAdditional = _partyRows.Any(r =>
                r.Hp != 0 || r.Atk != 0 || r.Def != 0 || r.Speed != 0 || r.SpAtk != 0 || r.SpDef != 0 ||
                r.Status != 0 || !string.IsNullOrWhiteSpace(r.PPCounts) || !string.IsNullOrWhiteSpace(r.Nickname) || r.BallSeal != 0 ||
                r.AdditionalFlags > 0);
            if (anyAdditional) flags.Add("TRAINER_DATA_TYPE_ADDITIONAL_FLAGS");

            return flags.Count == 0 ? "TRAINER_DATA_TYPE_NOTHING" : string.Join(" | ", flags);
        }

        // Optional helper to compute per-mon additionalflags number for future save wiring
        private static int ComputeAdditionalFlagsForMon(PartyRow r)
        {
            int af = 0;
            if (r.Status != 0) af |= 0x01; // TRAINER_DATA_EXTRA_TYPE_STATUS
            if (r.Hp != 0) af |= 0x02; // HP
            if (r.Atk != 0) af |= 0x04; // ATK
            if (r.Def != 0) af |= 0x08; // DEF
            if (r.Speed != 0) af |= 0x10; // SPEED
            if (r.SpAtk != 0) af |= 0x20; // SP_ATK
            if (r.SpDef != 0) af |= 0x40; // SP_DEF
            if (!string.IsNullOrWhiteSpace(r.PPCounts)) af |= 0x80; // PP_COUNTS
            if (!string.IsNullOrWhiteSpace(r.Nickname)) af |= 0x100; // NICKNAME
            // Types override lives under IV/EV set flag; no separate extra-type bit in constants.s
            return af;
        }

        private async void OnEditAIFlags(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                var allFlags = Data.HGParsers.AIFlagMacros.ToList();
                allFlags.Sort(StringComparer.Ordinal);
                var current = (AIFlagsBox.Text ?? string.Empty)
                    .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.Trim())
                    .ToHashSet(StringComparer.Ordinal);

                var panel = new StackPanel { Spacing = 6 };
                var checks = new List<CheckBox>();
                foreach (var flag in allFlags)
                {
                    var cb = new CheckBox { Content = flag, IsChecked = current.Contains(flag) };
                    checks.Add(cb);
                    panel.Children.Add(cb);
                }

                var dlg = new ContentDialog
                {
                    Title = "Select AI Flags",
                    PrimaryButtonText = "Apply",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot,
                    Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }
                };
                var res = await dlg.ShowAsync();
                if (res == ContentDialogResult.Primary)
                {
                    var selected = checks.Where(c => c.IsChecked == true).Select(c => c.Content?.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s));
                    AIFlagsBox.Text = string.Join(" | ", selected);
                }
            }
            catch { }
        }

        private async void OnShowAIFlagsHelp(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                // Minimal help text with the full list of flags pulled from constants.s
                var flags = string.Join("\n", Data.HGParsers.AIFlagMacros.OrderBy(s => s, StringComparer.Ordinal));
                var help = "Trainer AI flags (from armips/include/constants.s):\n\n" + flags + "\n\nCombine with | on the AI flags line.";
                var dlg = new ContentDialog { Title = "AI Flags", Content = help, PrimaryButtonText = "OK", XamlRoot = this.XamlRoot };
                await dlg.ShowAsync();
            }
            catch { }
        }

        private async void OnRestoreTrainerBackup(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (ProjectContext.RootPath == null) return;
            var path = System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            var backup = path + ".bak";
            if (!System.IO.File.Exists(backup))
            {
                var dlg = new ContentDialog { Title = "No backup found", CloseButtonText = "OK", XamlRoot = this.XamlRoot };
                await dlg.ShowAsync();
                return;
            }
            System.IO.File.Copy(backup, path, true);
            await LoadUpdatedAsync();
        }

        private async Task LoadUpdatedAsync()
        {
            await Data.HGParsers.RefreshTrainersAsync();
            _all = Data.HGParsers.Trainers.Select(t => $"{t.Id}: {t.Name} [{t.Class}] mons={t.NumMons} AI={t.AIFlags} Type={t.BattleType} Items={string.Join(",", t.Items)}").ToList();
            ApplyFilter();
        }
    }
}


