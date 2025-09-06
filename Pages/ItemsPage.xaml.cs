using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using HGEngineGUI.Data;

namespace HGEngineGUI.Pages
{
    public sealed partial class ItemsPage : Page
    {
        private List<(string ItemMacro, int Price)> _items = new();
        private (string ItemMacro, int Price)? _selected;
        private List<HGEngineGUI.Data.HGParsers.ItemDataEntry> _itemsData = new();
        private HGEngineGUI.Data.HGParsers.ItemDataEntry? _selectedData;
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
            _itemsData = HGEngineGUI.Data.HGParsers.ItemsData.ToList();

            // Populate dropdown sources
            HoldEffectBox.ItemsSource = HGEngineGUI.Data.HGParsers.HoldEffectMacros;
            NatGiftTypeBox.ItemsSource = HGEngineGUI.Data.HGParsers.TypeMacros;
            FieldPocketBox.ItemsSource = HGEngineGUI.Data.HGParsers.PocketMacros;
            BattlePocketBox.ItemsSource = HGEngineGUI.Data.HGParsers.BattlePocketMacros;
            FieldUseFuncBox.ItemsSource = HGEngineGUI.Data.HGParsers.UseFunctionDisplayLabels;
            BattleUseFuncBox.ItemsSource = HGEngineGUI.Data.HGParsers.UseFunctionDisplayLabels;
            // Effect lists (ID: Name)
            PluckEffectBox.ItemsSource = EffectCatalog.PluckLabels;
            FlingEffectBox.ItemsSource = EffectCatalog.FlingLabels;

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

        private void OnFieldUseFuncChanged(object sender, SelectionChangedEventArgs e)
        {
            var text = FieldUseFuncBox.Text ?? string.Empty;
            FieldUseFuncInfo.Text = DescribeUseFunc(text);
        }

        private void OnBattleUseFuncChanged(object sender, SelectionChangedEventArgs e)
        {
            var text = BattleUseFuncBox.Text ?? string.Empty;
            BattleUseFuncInfo.Text = DescribeUseFunc(text);
        }

        private static string DescribeUseFunc(string label)
        {
            return UseFuncDescriptions.Describe(label);
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
            _selectedData = _itemsData.FirstOrDefault(d => d.ItemMacro == macro);
            if (_selectedData != null)
            {
                ItemNameBox.Text = _selectedData.Name ?? string.Empty;
                ItemDescBox.Text = _selectedData.Description ?? string.Empty;
                HoldEffectBox.Text = _selectedData.HoldEffect;
                if (double.TryParse(_selectedData.HoldEffectParam, out var hefp)) HoldEffectParamBox.Value = hefp; else HoldEffectParamBox.Value = 0;
                PluckEffectBox.Text = EffectCatalog.IdToLabel(EffectCatalog.PluckLabels, _selectedData.PluckEffect);
                FlingEffectBox.Text = EffectCatalog.IdToLabel(EffectCatalog.FlingLabels, _selectedData.FlingEffect);
                FlingPowerBox.Text = _selectedData.FlingPower;
                NatGiftPowerBox.Text = _selectedData.NaturalGiftPower;
                NatGiftTypeBox.Text = _selectedData.NaturalGiftType;
                PreventTossBox.SelectedIndex = string.Equals(_selectedData.PreventToss, "TRUE", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                SelectableBox.SelectedIndex = string.Equals(_selectedData.Selectable, "TRUE", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                FieldPocketBox.Text = _selectedData.FieldPocket;
                BattlePocketBox.Text = _selectedData.BattlePocket;
                FieldUseFuncBox.Text = _selectedData.FieldUseFunc;
                BattleUseFuncBox.Text = _selectedData.BattleUseFunc;
                PartyUseBox.Text = _selectedData.PartyUse;
                // Populate numeric params
                SetParamBox(HpRestoreParamBox, "hp_restore_param");
                SetParamBox(PpRestoreParamBox, "pp_restore_param");
                SetParamBox(FriendshipLoParamBox, "friendship_mod_lo_param");
                SetParamBox(FriendshipMedParamBox, "friendship_mod_med_param");
                SetParamBox(FriendshipHiParamBox, "friendship_mod_hi_param");
                SetParamBox(HpEvUpParamBox, "hp_ev_up_param");
                SetParamBox(AtkEvUpParamBox, "atk_ev_up_param");
                SetParamBox(DefEvUpParamBox, "def_ev_up_param");
                SetParamBox(SpeedEvUpParamBox, "speed_ev_up_param");
                SetParamBox(SpatkEvUpParamBox, "spatk_ev_up_param");
                SetParamBox(SpdefEvUpParamBox, "spdef_ev_up_param");
                // Gather flags (prefer parsed cache; fall back to reading itemdata.c)
                var flags = GetFlagsForCurrentItem();
                // Diagnostics: read raw flags directly from itemdata.c to display source values
                try
                {
                    var fileFlags = HGEngineGUI.Data.HGParsers.TryReadPartyFlagsFromItemDataFile(_selectedData.ItemMacro);
                    var diags = new List<TextBlock>();
                    foreach (var kv in fileFlags.OrderBy(k => k.Key))
                    {
                        diags.Add(new TextBlock { Text = $"{kv.Key} = {kv.Value}" });
                    }
                    FlagDiagnosticsList.ItemsSource = diags;
                }
                catch { FlagDiagnosticsList.ItemsSource = null; }
                SetFlagCheckbox(FlagSlpHeal, flags, "slp_heal");
                SetFlagCheckbox(FlagPsnHeal, flags, "psn_heal");
                SetFlagCheckbox(FlagBrnHeal, flags, "brn_heal");
                SetFlagCheckbox(FlagFrzHeal, flags, "frz_heal");
                SetFlagCheckbox(FlagPrzHeal, flags, "prz_heal");
                SetFlagCheckbox(FlagCfsHeal, flags, "cfs_heal");
                SetFlagCheckbox(FlagInfHeal, flags, "inf_heal");
                SetFlagCheckbox(FlagGuardSpec, flags, "guard_spec");
                SetFlagCheckbox(FlagRevive, flags, "revive");
                SetFlagCheckbox(FlagReviveAll, flags, "revive_all");
                SetFlagCheckbox(FlagLevelUp, flags, "level_up");
                SetFlagCheckbox(FlagEvolve, flags, "evolve");
                SetFlagCheckbox(FlagPpUp, flags, "pp_up");
                SetFlagCheckbox(FlagPpMax, flags, "pp_max");
                SetFlagCheckbox(FlagPpRestore, flags, "pp_restore");
                SetFlagCheckbox(FlagPpRestoreAll, flags, "pp_restore_all");
                SetFlagCheckbox(FlagHpRestore, flags, "hp_restore");
                SetFlagCheckbox(FlagHpEvUp, flags, "hp_ev_up");
                SetFlagCheckbox(FlagAtkEvUp, flags, "atk_ev_up");
                SetFlagCheckbox(FlagDefEvUp, flags, "def_ev_up");
                SetFlagCheckbox(FlagSpeedEvUp, flags, "speed_ev_up");
                SetFlagCheckbox(FlagSpatkEvUp, flags, "spatk_ev_up");
                SetFlagCheckbox(FlagSpdefEvUp, flags, "spdef_ev_up");
                SetFlagCheckbox(FlagFriendshipModLo, flags, "friendship_mod_lo");
                SetFlagCheckbox(FlagFriendshipModMed, flags, "friendship_mod_med");
                SetFlagCheckbox(FlagFriendshipModHi, flags, "friendship_mod_hi");
            }
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

        private HGEngineGUI.Data.HGParsers.ItemDataEntry? CaptureItemDataFromUI()
        {
            if (_selectedData == null) return null;
            var d = new HGEngineGUI.Data.HGParsers.ItemDataEntry
            {
                ItemMacro = _selectedData.ItemMacro,
                ItemId = _selectedData.ItemId,
                Price = PriceBox.Text?.Trim() ?? _selectedData.Price,
                Name = ItemNameBox.Text ?? string.Empty,
                Description = ItemDescBox.Text ?? string.Empty,
                HoldEffect = (HoldEffectBox.Text ?? string.Empty).Trim(),
                HoldEffectParam = HoldEffectParamBox.Text?.Trim() ?? _selectedData.HoldEffectParam,
                PluckEffect = EffectCatalog.LabelToId(PluckEffectBox.Text ?? _selectedData.PluckEffect),
                FlingEffect = EffectCatalog.LabelToId(FlingEffectBox.Text ?? _selectedData.FlingEffect),
                FlingPower = FlingPowerBox.Text?.Trim() ?? _selectedData.FlingPower,
                NaturalGiftPower = NatGiftPowerBox.Text?.Trim() ?? _selectedData.NaturalGiftPower,
                NaturalGiftType = (NatGiftTypeBox.Text ?? string.Empty).Trim(),
                PreventToss = (PreventTossBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? _selectedData.PreventToss,
                Selectable = (SelectableBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? _selectedData.Selectable,
                FieldPocket = (FieldPocketBox.Text ?? string.Empty).Trim(),
                BattlePocket = (BattlePocketBox.Text ?? string.Empty).Trim(),
                FieldUseFunc = FieldUseFuncBox.Text?.Trim() ?? _selectedData.FieldUseFunc,
                BattleUseFunc = BattleUseFuncBox.Text?.Trim() ?? _selectedData.BattleUseFunc,
                PartyUse = PartyUseBox.Text?.Trim() ?? _selectedData.PartyUse,
                PartyFlags = new Dictionary<string, string>(_selectedData.PartyFlags),
                PartyParams = new Dictionary<string, string>(_selectedData.PartyParams)
            };
            // collect params from fields
            d.PartyFlags.Clear(); d.PartyParams.Clear();
            CollectParam(d, HpRestoreParamBox, "hp_restore_param");
            CollectParam(d, PpRestoreParamBox, "pp_restore_param");
            CollectParam(d, FriendshipLoParamBox, "friendship_mod_lo_param");
            CollectParam(d, FriendshipMedParamBox, "friendship_mod_med_param");
            CollectParam(d, FriendshipHiParamBox, "friendship_mod_hi_param");
            CollectParam(d, HpEvUpParamBox, "hp_ev_up_param");
            CollectParam(d, AtkEvUpParamBox, "atk_ev_up_param");
            CollectParam(d, DefEvUpParamBox, "def_ev_up_param");
            CollectParam(d, SpeedEvUpParamBox, "speed_ev_up_param");
            CollectParam(d, SpatkEvUpParamBox, "spatk_ev_up_param");
            CollectParam(d, SpdefEvUpParamBox, "spdef_ev_up_param");
            // collect boolean flags from checkboxes
            CollectFlag(d, FlagSlpHeal, "slp_heal");
            CollectFlag(d, FlagPsnHeal, "psn_heal");
            CollectFlag(d, FlagBrnHeal, "brn_heal");
            CollectFlag(d, FlagFrzHeal, "frz_heal");
            CollectFlag(d, FlagPrzHeal, "prz_heal");
            CollectFlag(d, FlagCfsHeal, "cfs_heal");
            CollectFlag(d, FlagInfHeal, "inf_heal");
            CollectFlag(d, FlagGuardSpec, "guard_spec");
            CollectFlag(d, FlagRevive, "revive");
            CollectFlag(d, FlagReviveAll, "revive_all");
            CollectFlag(d, FlagLevelUp, "level_up");
            CollectFlag(d, FlagEvolve, "evolve");
            CollectFlag(d, FlagPpUp, "pp_up");
            CollectFlag(d, FlagPpMax, "pp_max");
            CollectFlag(d, FlagPpRestore, "pp_restore");
            CollectFlag(d, FlagPpRestoreAll, "pp_restore_all");
            CollectFlag(d, FlagHpRestore, "hp_restore");
            CollectFlag(d, FlagHpEvUp, "hp_ev_up");
            CollectFlag(d, FlagAtkEvUp, "atk_ev_up");
            CollectFlag(d, FlagDefEvUp, "def_ev_up");
            CollectFlag(d, FlagSpeedEvUp, "speed_ev_up");
            CollectFlag(d, FlagSpatkEvUp, "spatk_ev_up");
            CollectFlag(d, FlagSpdefEvUp, "spdef_ev_up");
            CollectFlag(d, FlagFriendshipModLo, "friendship_mod_lo");
            CollectFlag(d, FlagFriendshipModMed, "friendship_mod_med");
            CollectFlag(d, FlagFriendshipModHi, "friendship_mod_hi");
            return d;
        }

        private Dictionary<string,bool> GetFlagsForCurrentItem()
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (_selectedData != null)
            {
                foreach (var kv in _selectedData.PartyFlags)
                {
                    var s = (kv.Value ?? string.Empty).Trim();
                    bool b = s.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || s == "1";
                    var key = (kv.Key ?? string.Empty).Trim();
                    result[key] = b;
                }

                // Override with values read from the robust file parser (source of truth)
                try
                {
                    var fileFlags = HGEngineGUI.Data.HGParsers.TryReadPartyFlagsFromItemDataFile(_selectedData.ItemMacro);
                    foreach (var kv in fileFlags)
                    {
                        var key = (kv.Key ?? string.Empty).Trim();
                        var sval = (kv.Value ?? string.Empty).Trim();
                        bool b = sval.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || sval == "1";
                        result[key] = b;
                    }
                }
                catch { }
            }
            return result;
        }

        private void SetFlagCheckbox(CheckBox cb, Dictionary<string,bool> flags, string key)
        {
            if (flags.TryGetValue(key, out var b)) cb.IsChecked = b; else cb.IsChecked = false;
        }

        private void CollectFlag(HGEngineGUI.Data.HGParsers.ItemDataEntry d, CheckBox cb, string key)
        {
            d.PartyFlags[key] = (cb.IsChecked == true) ? "TRUE" : "FALSE";
        }

        private void SetParamBox(NumberBox box, string key)
        {
            if (_selectedData == null) { box.Value = 0; return; }
            if (_selectedData.PartyParams.TryGetValue(key, out var v))
            {
                if (double.TryParse(v, out var dv)) box.Value = dv; else box.Value = 0;
            }
            else
            {
                box.Value = 0;
            }
        }

        private void CollectParam(HGEngineGUI.Data.HGParsers.ItemDataEntry d, NumberBox box, string key)
        {
            var val = box.Value;
            // Default to 0 if NaN
            if (double.IsNaN(val)) val = 0;
            d.PartyParams[key] = ((int)val).ToString();
        }

        private async void OnPreviewItemData(object sender, RoutedEventArgs e)
        {
            var d = CaptureItemDataFromUI();
            if (d == null) return;
            var diff = await HGEngineGUI.Data.HGSerializers.PreviewItemDataAsync(new List<HGEngineGUI.Data.HGParsers.ItemDataEntry> { d });
            await ShowDiffAsync(diff);
        }

        private async void OnSaveItemData(object sender, RoutedEventArgs e)
        {
            var d = CaptureItemDataFromUI();
            if (d == null) return;
            await HGEngineGUI.Data.HGSerializers.SaveItemDataAsync(new List<HGEngineGUI.Data.HGParsers.ItemDataEntry> { d });
            await InitializeAsync();
        }

        private async void OnPreviewItemText(object sender, RoutedEventArgs e)
        {
            if (_selectedData == null) return;
            var change = (ItemId: _selectedData.ItemId, Name: ItemNameBox.Text, Description: ItemDescBox.Text);
            var diff = await HGEngineGUI.Data.HGSerializers.PreviewItemTextAsync(new List<(int, string?, string?)> { change });
            await ShowDiffAsync(diff);
        }

        private async void OnSaveItemText(object sender, RoutedEventArgs e)
        {
            if (_selectedData == null) return;
            var change = (ItemId: _selectedData.ItemId, Name: ItemNameBox.Text, Description: ItemDescBox.Text);
            await HGEngineGUI.Data.HGSerializers.SaveItemTextAsync(new List<(int, string?, string?)> { change });
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

    // Converts the combo label into a concise tooltip (re-uses DescribeUseFunc)
    public class UseFuncLabelToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var text = value as string ?? string.Empty;
            // Reuse static method to produce description text
            return UseFuncDescriptions.Describe(text);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public static class UseFuncDescriptions
    {
        public static string Describe(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return string.Empty;
            var cleaned = label.Replace(" (unused)", string.Empty);
            if (!cleaned.Contains(":")) return string.Empty;
            var parts = cleaned.Split(':');
            var right = parts.Length > 1 ? parts[1] : string.Empty;
            string desc = string.Empty;
            if (right.Contains("ItemFieldUseFunc_Bicycle")) desc = "Bicycle: mount/dismount on the field (with checks).";
            else if (right.Contains("ItemMenuUseFunc_HealingItem")) desc = "Healing (potions/status) via menu only.";
            else if (right.Contains("ItemMenuUseFunc_TMHM")) desc = "TM/HM usage UI (menu).";
            else if (right.Contains("ItemMenuUseFunc_Mail")) desc = "Mail UI (menu).";
            else if (right.Contains("ItemMenuUseFunc_Berry")) desc = "Berry usage from menu (with check).";
            else if (right.Contains("ItemMenuUseFunc_PalPad") || right.Contains("ItemFieldUseFunc_PalPad")) desc = "Opens Pal Pad on field.";
            else if (right.Contains("OldRod") || right.Contains("GoodRod") || right.Contains("SuperRod")) desc = "Starts fishing on field (requires facing water).";
            else if (right.Contains("ItemMenuUseFunc_EvoStone")) desc = "Evolution stone usage from menu.";
            else if (right.Contains("ItemMenuUseFunc_EscapeRope")) desc = "Escape Rope logic (menu with map check).";
            else if (right.Contains("ApricornBox")) desc = "Opens Apricorn Box on field.";
            else if (right.Contains("BerryPots")) desc = "Opens Berry Pots on field.";
            else if (right.Contains("UnownReport")) desc = "Opens Unown Report on field.";
            else if (right.Contains("DowsingMchn")) desc = "Opens Dowsing Machine on field.";
            else if (right.Contains("GbSounds")) desc = "Toggles GB Sounds (retro music) on field.";
            else if (right.Contains("Gracidea")) desc = "Gracidea: Changes Shaymin form (field).";
            else if (right.Contains("VSRecorder")) desc = "Opens VS Recorder on field.";
            else if (right.Contains("RevealGlass")) desc = "Reveal Glass UI (form toggle).";
            else if (right.Contains("DNASplicers")) desc = "DNA Splicers UI (Kyurem fuse/unfuse).";
            else if (right.Contains("AbilityCapsule")) desc = "Ability Capsule usage from menu.";
            else if (right.Contains("Mint")) desc = "Nature Mint usage from menu.";
            else if (right.Contains("Nectar")) desc = "Nectar usage from menu.";
            else if (right.Contains("ItemFieldUseFunc_Generic") && right.Contains("menu=NULL")) desc = "No field/menu behavior (placeholder).";
            return desc;
        }
    }

    public class EffectLabelToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var text = value as string ?? string.Empty;
            return EffectCatalog.Describe(text);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public static class EffectCatalog
    {
        private static readonly Dictionary<int, string> s_pluck = new()
        {
            {0, "None"},
            {1, "Cures Paralysis"},
            {2, "Cures Sleep"},
            {3, "Cures Poison"},
            {4, "Cures Burn"},
            {5, "Cures Freeze"},
            {6, "Restores 10 PP"},
            {7, "Restores 10 HP"},
            {8, "Cures Confusion"},
            {9, "Cures Any Status"},
            {10, "Restores 25% HP"},
            {11, "Spicy (+Atk, -Atk)"},
            {12, "Dry (+Def, -Def)"},
            {13, "Sweet (+Sp.Atk, -Sp.Atk)"},
            {14, "Bitter (+Sp.Def, -Sp.Def)"},
            {15, "Sour (+Speed, -Speed)"},
            {16, "+1 Attack (≤25% HP)"},
            {17, "+1 Defense (≤25% HP)"},
            {18, "+1 Speed (≤25% HP)"},
            {19, "+1 Sp.Atk (≤25% HP)"},
            {20, "+1 Sp.Def (≤25% HP)"},
            {21, "+1 Crit Rate (≤25% HP)"},
            {22, "+1 Random Stat (≤25% HP)"},
            {23, "+1 Accuracy (≤25% HP)"},
        };

        private static readonly Dictionary<int, string> s_fling = CreateFling();

        private static Dictionary<int, string> CreateFling()
        {
            // Fling shares 0-23 with Pluck effects, then adds 24-30 uniques
            var d = new Dictionary<int, string>(s_pluck);
            d[24] = "WhiteHerb";
            d[25] = "MentalHerb";
            d[26] = "RazorFang";
            d[27] = "LightBall";
            d[28] = "PoisonBarb";
            d[29] = "ToxicOrb";
            d[30] = "FlameOrb";
            return d;
        }

        public static IReadOnlyList<string> PluckLabels { get; } = BuildLabels(s_pluck);
        public static IReadOnlyList<string> FlingLabels { get; } = BuildLabels(s_fling);

        private static List<string> BuildLabels(Dictionary<int,string> map)
        {
            return map.OrderBy(k => k.Key).Select(k => $"{k.Key}: {k.Value}").ToList();
        }

        public static string Describe(string label)
        {
            // Tooltip equals the right-hand name
            if (string.IsNullOrWhiteSpace(label)) return string.Empty;
            var parts = label.Split(':');
            if (parts.Length < 2) return string.Empty;
            return parts[1].Trim();
        }

        public static string IdToLabel(IReadOnlyList<string> labels, string? idText)
        {
            if (!int.TryParse(idText, out var id)) return idText ?? string.Empty;
            var match = labels.FirstOrDefault(s => s.StartsWith(id.ToString() + ":", StringComparison.Ordinal));
            return match ?? id.ToString();
        }

        public static string LabelToId(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return "0";
            var idx = label.IndexOf(':');
            if (idx > 0)
            {
                var left = label.Substring(0, idx).Trim();
                if (int.TryParse(left, out var id)) return id.ToString();
            }
            // Fallback if user typed a number directly
            if (int.TryParse(label.Trim(), out var id2)) return id2.ToString();
            return "0";
        }
    }
}



