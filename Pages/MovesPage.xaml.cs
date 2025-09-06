using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HGEngineGUI.Data;

namespace HGEngineGUI.Pages
{
    public sealed partial class MovesPage : Page
    {
        private List<HGEngineGUI.Data.HGParsers.MoveEntry> _moves = new();
        private HGEngineGUI.Data.HGParsers.MoveEntry? _selected;

        public MovesPage()
        {
            this.InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await HGEngineGUI.Data.HGParsers.RefreshCachesAsync();
            await HGEngineGUI.Data.HGParsers.RefreshMovesAsync();
            _moves = HGEngineGUI.Data.HGParsers.Moves.ToList();
            MoveList.ItemsSource = _moves.Select(m => m.MoveMacro + " - " + m.Name).ToList();

            // Populate dropdown sources
            TypeBox.ItemsSource = HGEngineGUI.Data.HGParsers.TypeMacros;
            SplitBox.ItemsSource = HGEngineGUI.Data.HGParsers.MoveSplitMacros;
            EffectBox.ItemsSource = HGEngineGUI.Data.HGParsers.MoveEffectMacros;
            TargetBox.ItemsSource = HGEngineGUI.Data.HGParsers.MoveTargetMacros;
            ContestTypeBox.ItemsSource = HGEngineGUI.Data.HGParsers.ContestTypeMacros;
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text?.Trim() ?? string.Empty;
            IEnumerable<HGEngineGUI.Data.HGParsers.MoveEntry> src = _moves;
            if (!string.IsNullOrWhiteSpace(q))
            {
                src = _moves.Where(m => (m.MoveMacro?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (m.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            MoveList.ItemsSource = src.Select(m => m.MoveMacro + " - " + m.Name).ToList();
        }

        private void OnMoveClick(object sender, ItemClickEventArgs e)
        {
            var label = e.ClickedItem?.ToString() ?? string.Empty;
            var macro = label.Split(' ').FirstOrDefault() ?? string.Empty;
            _selected = _moves.FirstOrDefault(m => m.MoveMacro == macro);
            if (_selected == null) return;

            SelectedMoveText.Text = _selected.MoveMacro + " — " + _selected.Name;
            SelectedMoveIdText.Text = _selected.MoveId.ToString();
            NameBox.Text = _selected.Name;
            DescBox.Text = _selected.Description;
            TypeBox.Text = _selected.TypeMacro;
            SplitBox.Text = _selected.SplitMacro;
            PowerBox.Value = _selected.BasePower;
            AccuracyBox.Value = _selected.Accuracy;
            PpBox.Value = _selected.PP;
            PriorityBox.Value = _selected.Priority;
            EffectBox.Text = _selected.EffectMacro;
            SecChanceBox.Value = _selected.SecondaryEffectChance;
            TargetBox.Text = _selected.TargetMacro;
            AppealBox.Value = _selected.Appeal;
            ContestTypeBox.Text = _selected.ContestTypeMacro;

            SetFlag(FlagContact, "FLAG_CONTACT");
            SetFlag(FlagProtect, "FLAG_PROTECT");
            SetFlag(FlagMagicCoat, "FLAG_MAGIC_COAT");
            SetFlag(FlagSnatch, "FLAG_SNATCH");
            SetFlag(FlagMirrorMove, "FLAG_MIRROR_MOVE");
            SetFlag(FlagKingsRock, "FLAG_KINGS_ROCK");
            SetFlag(FlagKeepHpBar, "FLAG_KEEP_HP_BAR");
            SetFlag(FlagHideShadow, "FLAG_HIDE_SHADOW");

            // Where-used aggregation
            _ = LoadUsagesAsync(_selected.MoveMacro);
        }

        private void SetFlag(CheckBox cb, string macro)
        {
            if (_selected == null) { cb.IsChecked = false; return; }
            cb.IsChecked = _selected.FlagMacros.Contains(macro);
        }

        private async Task LoadUsagesAsync(string moveMacro)
        {
            try
            {
                var usages = await HGEngineGUI.Data.HGParsers.FindMoveUsagesAsync(moveMacro);
                UsageList.ItemsSource = usages.Select(u => new TextBlock { Text = u }).ToList();
            }
            catch
            {
                UsageList.ItemsSource = null;
            }
        }

        private HGEngineGUI.Data.HGParsers.MoveEntry? CaptureFromUI()
        {
            if (_selected == null) return null;
            var m = new HGEngineGUI.Data.HGParsers.MoveEntry
            {
                MoveMacro = _selected.MoveMacro,
                MoveId = _selected.MoveId,
                Name = NameBox.Text ?? string.Empty,
                Description = DescBox.Text ?? string.Empty,
                TypeMacro = (TypeBox.Text ?? string.Empty).Trim(),
                SplitMacro = (SplitBox.Text ?? string.Empty).Trim(),
                BasePower = (int)PowerBox.Value,
                Accuracy = (int)AccuracyBox.Value,
                PP = (int)PpBox.Value,
                Priority = (int)PriorityBox.Value,
                EffectMacro = (EffectBox.Text ?? string.Empty).Trim(),
                SecondaryEffectChance = (int)SecChanceBox.Value,
                TargetMacro = (TargetBox.Text ?? string.Empty).Trim(),
                Appeal = (int)AppealBox.Value,
                ContestTypeMacro = (ContestTypeBox.Text ?? string.Empty).Trim(),
                FlagMacros = new List<string>()
            };
            void AddFlag(CheckBox cb, string macro) { if (cb.IsChecked == true) m.FlagMacros.Add(macro); }
            AddFlag(FlagContact, "FLAG_CONTACT");
            AddFlag(FlagProtect, "FLAG_PROTECT");
            AddFlag(FlagMagicCoat, "FLAG_MAGIC_COAT");
            AddFlag(FlagSnatch, "FLAG_SNATCH");
            AddFlag(FlagMirrorMove, "FLAG_MIRROR_MOVE");
            AddFlag(FlagKingsRock, "FLAG_KINGS_ROCK");
            AddFlag(FlagKeepHpBar, "FLAG_KEEP_HP_BAR");
            AddFlag(FlagHideShadow, "FLAG_HIDE_SHADOW");
            m.FlagsRaw = m.FlagMacros.Count > 0 ? string.Join(" | ", m.FlagMacros) : _selected.FlagsRaw;
            return m;
        }

        private async void OnPreviewMoves(object sender, RoutedEventArgs e)
        {
            var m = CaptureFromUI();
            if (m == null) return;
            var diff = await HGEngineGUI.Data.HGSerializers.PreviewMoveDataAsync(new List<HGEngineGUI.Data.HGParsers.MoveEntry> { m });
            await ShowDiffAsync(diff);
        }

        private async void OnSaveMoves(object sender, RoutedEventArgs e)
        {
            var m = CaptureFromUI();
            if (m == null) return;
            await HGEngineGUI.Data.HGSerializers.SaveMoveDataAsync(new List<HGEngineGUI.Data.HGParsers.MoveEntry> { m });
            await InitializeAsync();
        }

        // Future: Tutor editor UI; for now, the move editor covers core fields and where-used diagnostics.

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

        private async void OnOpenMoveAnim(object sender, RoutedEventArgs e)
        {
            await ProjectContext.OpenFolderAsync("armips/move/move_anim");
        }

        private async void OnOpenMoveSubAnim(object sender, RoutedEventArgs e)
        {
            await ProjectContext.OpenFolderAsync("armips/move/move_sub_anim");
        }

        private async void OnOpenEffectScripts(object sender, RoutedEventArgs e)
        {
            await ProjectContext.OpenFolderAsync("data/battle_scripts/effects");
        }
    }
}


