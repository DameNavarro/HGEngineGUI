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
                    PartyList.ItemsSource = _partyRows;

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
                // For now support saving basic fields (slot, species, level, ivs, abilityslot)
                await Data.HGSerializers.SaveTrainerPartyAsync(id, _partyRows);
                await LoadUpdatedAsync();
            }
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


