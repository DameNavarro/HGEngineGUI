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
                    await Data.HGParsers.RefreshTrainerDetailsAsync(id);
                    var header = Data.HGParsers.Trainers.FirstOrDefault(t => t.Id == id);
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
                var container = new StackPanel { Spacing = 6, Padding = new Microsoft.UI.Xaml.Thickness(4) };

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

                var abilityStack = new StackPanel { Width = 120 };
                abilityStack.Children.Add(new TextBlock { Text = "AbilitySlot", FontSize = 12 });
                var abilityBox = new TextBox { Text = row.AbilitySlot.ToString() };
                abilityBox.TextChanging += (s, e) => { if (int.TryParse(abilityBox.Text, out var v)) row.AbilitySlot = v; };
                abilityStack.Children.Add(abilityBox);
                topGrid.Children.Add(abilityStack);
                Grid.SetColumn(abilityStack, 4);

                var itemStack = new StackPanel { Width = 240 };
                itemStack.Children.Add(new TextBlock { Text = "Item", FontSize = 12 });
                var itemCombo = new ComboBox { IsEditable = true, ItemsSource = ItemMacroList, SelectedItem = row.Item };
                itemCombo.SelectionChanged += (s, e) => { row.Item = (itemCombo.SelectedItem as string) ?? (itemCombo.Text ?? string.Empty); };
                itemCombo.LostFocus += (s, e) => { row.Item = itemCombo.Text ?? string.Empty; };
                itemStack.Children.Add(itemCombo);
                topGrid.Children.Add(itemStack);
                Grid.SetColumn(itemStack, 5);

                container.Children.Add(topGrid);

                var moveGrid = new Grid { ColumnSpacing = 8 };
                for (int i = 0; i < 4; i++) moveGrid.ColumnDefinitions.Add(new ColumnDefinition());
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
                var natureStack = new StackPanel(); natureStack.Children.Add(new TextBlock { Text = "Nature", FontSize = 12 }); var natureBox = new TextBox { Text = row.Nature }; natureBox.TextChanging += (s, e) => row.Nature = natureBox.Text; natureStack.Children.Add(natureBox); miscGrid.Children.Add(natureStack); Grid.SetColumn(natureStack, 0);
                var formStack = new StackPanel(); formStack.Children.Add(new TextBlock { Text = "Form", FontSize = 12 }); var formBox = new TextBox { Text = row.Form.ToString() }; formBox.TextChanging += (s, e) => { if (int.TryParse(formBox.Text, out var v)) row.Form = v; }; formStack.Children.Add(formBox); miscGrid.Children.Add(formStack); Grid.SetColumn(formStack, 1);
                var ballStack = new StackPanel(); ballStack.Children.Add(new TextBlock { Text = "Ball", FontSize = 12 }); var ballBox = new TextBox { Text = row.Ball }; ballBox.TextChanging += (s, e) => row.Ball = ballBox.Text; ballStack.Children.Add(ballBox); miscGrid.Children.Add(ballStack); Grid.SetColumn(ballStack, 2);
                var shinyStack = new StackPanel(); shinyStack.Children.Add(new TextBlock { Text = "Shiny Lock", FontSize = 12 }); var shinyBox = new CheckBox { IsChecked = row.ShinyLock }; shinyBox.Checked += (s, e) => row.ShinyLock = true; shinyBox.Unchecked += (s, e) => row.ShinyLock = false; shinyStack.Children.Add(shinyBox); miscGrid.Children.Add(shinyStack); Grid.SetColumn(shinyStack, 3);
                container.Children.Add(miscGrid);

                var lastGrid = new Grid { ColumnSpacing = 8 };
                lastGrid.ColumnDefinitions.Add(new ColumnDefinition());
                lastGrid.ColumnDefinitions.Add(new ColumnDefinition());
                var ppStack = new StackPanel(); ppStack.Children.Add(new TextBlock { Text = "PP (comma-separated)", FontSize = 12 }); var ppBox = new TextBox { Text = row.PP }; ppBox.TextChanging += (s, e) => row.PP = ppBox.Text; ppStack.Children.Add(ppBox); lastGrid.Children.Add(ppStack); Grid.SetColumn(ppStack, 0);
                var nickStack = new StackPanel(); nickStack.Children.Add(new TextBlock { Text = "Nickname", FontSize = 12 }); var nickBox = new TextBox { Text = row.Nickname }; nickBox.TextChanging += (s, e) => row.Nickname = nickBox.Text; nickStack.Children.Add(nickBox); lastGrid.Children.Add(nickStack); Grid.SetColumn(nickStack, 1);
                container.Children.Add(lastGrid);

                PartyStack.Children.Add(container);
            }
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
                PP = r.PP ?? string.Empty
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
                await Data.HGSerializers.SaveTrainerHeaderAsync(id, cls, ai, bt, items);
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
            sb.AppendLine($"\n    trainermontype TRAINER_DATA_TYPE_NOTHING");
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


