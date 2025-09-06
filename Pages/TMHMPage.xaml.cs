using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HGEngineGUI.Pages
{
    public sealed partial class TMHMPage : Page
    {
        private List<string> _headers = new();
        private List<string> _moveMacros = new();

        public TMHMPage()
        {
            this.InitializeComponent();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await HGEngineGUI.Data.HGParsers.RefreshCachesAsync();

            _headers = HGEngineGUI.Data.HGParsers.TmHmMoves.ToList();
            TmSelector.ItemsSource = _headers;

            _moveMacros = HGEngineGUI.Data.HGParsers.MoveMacros.ToList();
            MoveSelector.ItemsSource = _moveMacros;

            if (_headers.Count > 0)
            {
                TmSelector.SelectedIndex = 0;
                await RefreshSelectedAsync();
            }
        }

        private async Task RefreshSelectedAsync()
        {
            var header = TmSelector.SelectedItem as string ?? string.Empty;
            CurrentHeader.Text = header;
            var species = await LoadSpeciesForHeaderAsync(header);
            SpeciesList.ItemsSource = species;
            SpeciesCount.Text = $"({species.Count} species)";
            PreviewText.Text = string.Empty;
            PreviewPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        private async Task<List<string>> LoadSpeciesForHeaderAsync(string header)
        {
            var list = new List<string>();
            try
            {
                var path = HGEngineGUI.Data.HGParsers.PathTm ?? System.IO.Path.Combine(ProjectContext.RootPath!, "armips", "data", "tmlearnset.txt");
                if (!System.IO.File.Exists(path)) return list;
                var text = (await System.IO.File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
                var headers = Regex.Matches(text, @"^(TM|HM)\d{3}:\s+[A-Z0-9_]+\s*$", RegexOptions.Multiline).Cast<Match>().ToList();
                var m = headers.FirstOrDefault(mm => mm.Value.Trim() == header);
                if (m == null) return list;
                int start = m.Index;
                int idx = headers.FindIndex(mm => mm.Index == m.Index);
                int end = (idx + 1 < headers.Count) ? headers[idx + 1].Index : text.Length;
                string block = text.Substring(start, end - start);
                foreach (Match sm in Regex.Matches(block, @"^\s*SPECIES_[A-Z0-9_]+\s*$", RegexOptions.Multiline))
                {
                    list.Add(sm.Value.Trim());
                }
            }
            catch { }
            return list;
        }

        private async void OnPreview(object sender, RoutedEventArgs e)
        {
            var header = TmSelector.SelectedItem as string;
            var newMove = MoveSelector.SelectedItem as string ?? MoveSelector.Text?.Trim();
            if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(newMove)) return;

            var diff = await PreviewChangeAsync(header!, newMove!);
            PreviewText.Text = diff;
            PreviewPanel.Visibility = string.IsNullOrWhiteSpace(diff) ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
        }

        private async Task<string> PreviewChangeAsync(string header, string newMoveMacro)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGEngineGUI.Data.HGParsers.PathTm ?? System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "tmlearnset.txt");
            if (!System.IO.File.Exists(path)) return string.Empty;
            var original = (await System.IO.File.ReadAllTextAsync(path)).Replace("\r\n", "\n");

            // Replace just the header line's move macro
            // Header format: TMxxx: MOVE_FOO
            string pattern = "^" + Regex.Escape(header.Split(':')[0]) + @":\s+[A-Z0-9_]+\s*$";
            var rx = new Regex(pattern, RegexOptions.Multiline);
            if (!rx.IsMatch(original)) return string.Empty;
            string updated = rx.Replace(original, m =>
            {
                var left = header.Split(':')[0];
                return left + ": " + newMoveMacro;
            }, 1);

            return HGEngineGUI.Data.HGSerializers.ComputeUnifiedDiff(original, updated, "tmlearnset.txt");
        }

        private async void OnSave(object sender, RoutedEventArgs e)
        {
            var header = TmSelector.SelectedItem as string;
            var newMove = MoveSelector.SelectedItem as string ?? MoveSelector.Text?.Trim();
            if (string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(newMove)) return;

            await SaveChangeAsync(header!, newMove!);
            await HGEngineGUI.Data.HGParsers.RefreshCachesAsync();
            _headers = HGEngineGUI.Data.HGParsers.TmHmMoves.ToList();
            TmSelector.ItemsSource = _headers;
            TmSelector.SelectedItem = _headers.FirstOrDefault(h => h.StartsWith(header.Split(':')[0] + ":", StringComparison.Ordinal));
            await RefreshSelectedAsync();
        }

        private async Task SaveChangeAsync(string header, string newMoveMacro)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGEngineGUI.Data.HGParsers.PathTm ?? System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "tmlearnset.txt");
            if (!System.IO.File.Exists(path)) return;
            var text = (await System.IO.File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
            string pattern = "^" + Regex.Escape(header.Split(':')[0]) + @":\s+[A-Z0-9_]+\s*$";
            var rx = new Regex(pattern, RegexOptions.Multiline);
            if (!rx.IsMatch(text)) return;
            string updated = rx.Replace(text, m =>
            {
                var left = header.Split(':')[0];
                return left + ": " + newMoveMacro;
            }, 1);

            // backup and write
            var backup = path + ".bak";
            try { System.IO.File.Copy(path, backup, true); } catch { }
            await System.IO.File.WriteAllTextAsync(path, updated);
            HGEngineGUI.Services.ChangeLog.Record(path, updated.Length);
        }

        private async void OnTmChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            await RefreshSelectedAsync();
        }

        private void OnMoveChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            var move = MoveSelector.SelectedItem as string ?? string.Empty;
            MoveMeta.Text = DescribeMoveMeta(move);
        }

        private string DescribeMoveMeta(string moveMacro)
        {
            try
            {
                var entry = HGEngineGUI.Data.HGParsers.Moves.FirstOrDefault(m => string.Equals(m.MoveMacro, moveMacro, StringComparison.OrdinalIgnoreCase));
                if (entry == null) return string.Empty;
                var type = entry.TypeMacro;
                var pow = entry.BasePower;
                var acc = entry.Accuracy;
                var pp = entry.PP;
                return $"{type} • Pow {pow} • Acc {acc} • PP {pp}";
            }
            catch { return string.Empty; }
        }
    }
}


