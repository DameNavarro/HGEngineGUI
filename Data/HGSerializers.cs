using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HGEngineGUI.Data
{
    public static class HGSerializers
    {
        private static string EscapeQuotes(string s) => s.Replace("\"", "\\\"");
        // Basic diff preview utility
        public static string ComputeUnifiedDiff(string original, string updated, string header = "changes")
        {
            // Minimal line-by-line diff: show +/- for changed lines
            var o = original.Replace("\r\n", "\n").Split('\n');
            var u = updated.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder();
            sb.AppendLine($"--- a/{header}");
            sb.AppendLine($"+++ b/{header}");
            int max = Math.Max(o.Length, u.Length);
            for (int i = 0; i < max; i++)
            {
                string ol = i < o.Length ? o[i] : string.Empty;
                string ul = i < u.Length ? u[i] : string.Empty;
                if (ol == ul)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(ol)) sb.AppendLine($"- {ol}");
                if (!string.IsNullOrEmpty(ul)) sb.AppendLine($"+ {ul}");
            }
            return sb.ToString();
        }
        // Save Level-up section for a species in armips/data/levelupdata.s
        public static async Task SaveLevelUpAsync(string speciesMacro, List<(int level, string move)> entries)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGParsers.PathLevelUp ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "levelupdata.s");
            if (!File.Exists(path)) return;

            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"levelup {speciesMacro}";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return;
            // Replace the entire block until the next levelup header to avoid duplicate terminators
            int nextIdx = text.IndexOf("\n\nlevelup ", startIdx + blockStart.Length, StringComparison.Ordinal);
            if (nextIdx < 0) nextIdx = text.IndexOf("levelup ", startIdx + blockStart.Length, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;

            var sb = new StringBuilder();
            sb.AppendLine(blockStart);
            foreach (var (level, move) in entries)
            {
                sb.AppendLine($"    learnset {move}, {level}");
            }
            sb.AppendLine("    terminatelearnset");
            sb.AppendLine();

            string newText = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            await File.WriteAllTextAsync(path, newText);
            HGEngineGUI.Services.ChangeLog.Record(path, newText.Length);
        }

        // Preview helpers (non-writing) for unified diff
        public static async Task<string> PreviewLevelUpAsync(string speciesMacro, List<(int level, string move)> entries)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathLevelUp ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "levelupdata.s");
            if (!File.Exists(path)) return string.Empty;
            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"levelup {speciesMacro}";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return string.Empty;
            // Preview by replacing up to the next block header
            int nextIdx = text.IndexOf("\n\nlevelup ", startIdx + blockStart.Length, StringComparison.Ordinal);
            if (nextIdx < 0) nextIdx = text.IndexOf("levelup ", startIdx + blockStart.Length, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;
            var sb = new StringBuilder();
            sb.AppendLine(blockStart);
            foreach (var (level, move) in entries)
            {
                sb.AppendLine($"    learnset {move}, {level}");
            }
            sb.AppendLine("    terminatelearnset");
            sb.AppendLine();
            string updated = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            return ComputeUnifiedDiff(text, updated, "levelupdata.s");
        }

        public static async Task<string> PreviewEggMovesAsync(string speciesMacro, List<string> moves)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathEgg ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "eggmoves.s");
            if (!File.Exists(path)) return string.Empty;
            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"eggmoveentry {speciesMacro}";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return string.Empty;
            int nextIdx = text.IndexOf("eggmoveentry ", startIdx + blockStart.Length, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;
            var sb = new StringBuilder();
            sb.AppendLine(blockStart);
            foreach (var m in moves) sb.AppendLine($"    eggmove {m}");
            sb.AppendLine();
            string updated = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            return ComputeUnifiedDiff(text, updated, "eggmoves.s");
        }

        public static async Task<string> PreviewEvolutionsAsync(string speciesMacro, List<(string method, int param, string target)> evolutions)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathEvo ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "evodata.s");
            if (!File.Exists(path)) return string.Empty;
            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"evodata {speciesMacro}";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return string.Empty;
            // Replace entire evodata block up to next header to avoid duplicate terminators
            int nextIdx = text.IndexOf("\n\nevodata ", startIdx + blockStart.Length, StringComparison.Ordinal);
            if (nextIdx < 0) nextIdx = text.IndexOf("evodata ", startIdx + blockStart.Length, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;
            var sb = new StringBuilder();
            sb.AppendLine(blockStart);
            foreach (var (method, param, target) in evolutions)
            {
                sb.AppendLine($"    evolution {method}, {param}, {target}");
            }
            int count = evolutions.Count; while (count++ < 9) sb.AppendLine("    evolution EVO_NONE, 0, SPECIES_NONE");
            sb.AppendLine("    terminateevodata");
            sb.AppendLine();
            string updated = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            return ComputeUnifiedDiff(text, updated, "evodata.s");
        }

        public static async Task<string> PreviewTmHmAsync(string speciesMacro, List<string> selectedTmLabels)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathTm ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "tmlearnset.txt");
            if (!File.Exists(path)) return string.Empty;
            var lines = (await File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
            var blockRegex = new Regex("^(TM|HM)\\d{3}:.*$", RegexOptions.Multiline);
            var matches = blockRegex.Matches(lines);
            var builder = new StringBuilder();
            int cursor = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                int nextStart = (i + 1 < matches.Count) ? matches[i + 1].Index : lines.Length;
                builder.Append(lines.Substring(cursor, m.Index - cursor));
                var block = lines.Substring(m.Index, nextStart - m.Index);
                var headerLine = block.Split('\n')[0].Trim();
                var speciesSet = new HashSet<string>(StringComparer.Ordinal);
                var speciesLineRegex = new Regex(@"^\s*SPECIES_[A-Z0-9_]+\s*$", RegexOptions.Multiline);
                foreach (Match sm in speciesLineRegex.Matches(block)) speciesSet.Add(sm.Value.Trim());
                bool shouldHave = selectedTmLabels.Contains(headerLine);
                string speciesLine = "    " + speciesMacro;
                if (shouldHave) speciesSet.Add(speciesLine); else speciesSet.Remove(speciesLine);
                builder.AppendLine(headerLine);
                foreach (var s in speciesSet.OrderBy(s => s)) builder.AppendLine(s);
                builder.AppendLine();
                cursor = nextStart;
            }
            var updated = builder.ToString();
            return ComputeUnifiedDiff(lines, updated, "tmlearnset.txt");
        }

        public static async Task<string> PreviewTutorsAsync(string speciesMacro, List<(string tutor, string move, int cost)> selected)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathTutor ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "tutordata.txt");
            if (!File.Exists(path)) return string.Empty;
            var lines = (await File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
            var tutorHeaderRegex = new Regex(@"^TUTOR_[A-Z0-9_]+:\s+[A-Z0-9_]+\s+\d+\s*$", RegexOptions.Multiline);
            var headers = tutorHeaderRegex.Matches(lines);
            var sb = new StringBuilder();
            int cursor = 0;
            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i];
                int nextStart = (i + 1 < headers.Count) ? headers[i + 1].Index : lines.Length;
                sb.Append(lines.Substring(cursor, h.Index - cursor));
                var block = lines.Substring(h.Index, nextStart - h.Index);
                bool shouldHave = selected.Any(s => block.StartsWith($"{s.tutor}: {s.move} {s.cost}", StringComparison.Ordinal));
                var speciesSet = new HashSet<string>(StringComparer.Ordinal);
                var speciesLineRegex = new Regex(@"^\s*SPECIES_[A-Z0-9_]+\s*$", RegexOptions.Multiline);
                foreach (Match sm in speciesLineRegex.Matches(block)) speciesSet.Add(sm.Value.Trim());
                string speciesLine = "    " + speciesMacro;
                if (shouldHave) speciesSet.Add(speciesLine); else speciesSet.Remove(speciesLine);
                var headerLine = block.Split('\n')[0].Trim();
                sb.AppendLine(headerLine);
                foreach (var s in speciesSet.OrderBy(s => s)) sb.AppendLine(s);
                sb.AppendLine();
                cursor = nextStart;
            }
            var updated = sb.ToString();
            return ComputeUnifiedDiff(lines, updated, "tutordata.txt");
        }

        // Hidden Ability table (data/HiddenAbilityTable.c)
        public static async Task<string> PreviewHiddenAbilityAsync(string speciesMacro, string abilityMacro)
        {
            if (ProjectContext.RootPath == null || string.IsNullOrWhiteSpace(abilityMacro)) return string.Empty;
            var path = Path.Combine(ProjectContext.RootPath, "data", "HiddenAbilityTable.c");
            if (!File.Exists(path)) return string.Empty;
            var original = await File.ReadAllTextAsync(path);
            var pattern = new Regex(@"(\[\s*" + Regex.Escape(speciesMacro) + @"\s*\]\s*=\s*)(ABILITY_[A-Z0-9_]+)(\s*,)", RegexOptions.Multiline);
            if (!pattern.IsMatch(original)) return string.Empty;
            var updated = pattern.Replace(original, m => m.Groups[1].Value + abilityMacro + m.Groups[3].Value, 1);
            return ComputeUnifiedDiff(original, updated, "HiddenAbilityTable.c");
        }

        public static async Task SaveHiddenAbilityAsync(string speciesMacro, string abilityMacro)
        {
            if (ProjectContext.RootPath == null || string.IsNullOrWhiteSpace(abilityMacro)) return;
            var path = Path.Combine(ProjectContext.RootPath, "data", "HiddenAbilityTable.c");
            if (!File.Exists(path)) return;
            var text = await File.ReadAllTextAsync(path);
            var pattern = new Regex(@"(\[\s*" + Regex.Escape(speciesMacro) + @"\s*\]\s*=\s*)(ABILITY_[A-Z0-9_]+)(\s*,)", RegexOptions.Multiline);
            if (!pattern.IsMatch(text)) return;
            var updated = pattern.Replace(text, m => m.Groups[1].Value + abilityMacro + m.Groups[3].Value, 1);
            var backup = path + ".bak";
            try { File.Copy(path, backup, true); } catch { }
            await File.WriteAllTextAsync(path, updated);
            HGEngineGUI.Services.ChangeLog.Record(path, updated.Length);
        }

        // Overview save/preview
        public static async Task SaveOverviewAsync(string speciesMacro, HGParsers.SpeciesOverview ov)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGParsers.PathMondata ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "mondata.s");
            if (!File.Exists(path)) return;

            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"mondata {speciesMacro},";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return;
            int nextIdx = text.IndexOf("\n\nmondata ", startIdx, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;

            var sb = new StringBuilder();
            sb.AppendLine(blockStart + " \"\"");
            sb.AppendLine($"    basestats {ov.BaseHp}, {ov.BaseAttack}, {ov.BaseDefense}, {ov.BaseSpeed}, {ov.BaseSpAttack}, {ov.BaseSpDefense}");
            sb.AppendLine($"    types {ov.Type1}, {ov.Type2}");
            sb.AppendLine($"    catchrate {ov.CatchRate}");
            // BaseExp is managed in data/BaseExperienceTable.c; do not write in mondata.s
            sb.AppendLine($"    evyields {ov.EvYields.hp}, {ov.EvYields.atk}, {ov.EvYields.def}, {ov.EvYields.spd}, {ov.EvYields.spatk}, {ov.EvYields.spdef}");
            sb.AppendLine($"    items {ov.Item1}, {ov.Item2}");
            sb.AppendLine($"    genderratio {ov.GenderRatio}");
            sb.AppendLine($"    eggcycles {ov.EggCycles}");
            sb.AppendLine($"    basefriendship {ov.BaseFriendship}");
            sb.AppendLine($"    growthrate {ov.GrowthRate}");
            sb.AppendLine($"    egggroups {ov.EggGroup1}, {ov.EggGroup2}");
            sb.AppendLine($"    abilities {ov.Ability1}, {ov.Ability2}");
            if (ov.RunChance > 0) sb.AppendLine($"    runchance {ov.RunChance}");
            // Hidden ability is stored in data/HiddenAbilityTable.c and not in mondata.s
            if (!string.IsNullOrWhiteSpace(ov.DexClassification)) sb.AppendLine($"    mondexclassification {speciesMacro}, \"\"{EscapeQuotes(ov.DexClassification)}\"\"");
            if (!string.IsNullOrWhiteSpace(ov.DexEntry)) sb.AppendLine($"    mondexentry {speciesMacro}, \"\"{EscapeQuotes(ov.DexEntry)}\"\"");
            if (!string.IsNullOrWhiteSpace(ov.DexHeight)) sb.AppendLine($"    mondexheight {speciesMacro}, \"\"{EscapeQuotes(ov.DexHeight)}\"\"");
            if (!string.IsNullOrWhiteSpace(ov.DexWeight)) sb.AppendLine($"    mondexweight {speciesMacro}, \"\"{EscapeQuotes(ov.DexWeight)}\"\"");
            sb.AppendLine();

            var updated = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            var backup = path + ".bak";
            try { File.Copy(path, backup, true); } catch { }
            await File.WriteAllTextAsync(path, updated);
        }

        public static async Task<string> PreviewOverviewAsync(string speciesMacro, HGParsers.SpeciesOverview ov)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathMondata ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "mondata.s");
            if (!File.Exists(path)) return string.Empty;
            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"mondata {speciesMacro},";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return string.Empty;
            int nextIdx = text.IndexOf("\n\nmondata ", startIdx, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;

            var sb = new StringBuilder();
            sb.AppendLine(blockStart + " \"\"");
            sb.AppendLine($"    basestats {ov.BaseHp}, {ov.BaseAttack}, {ov.BaseDefense}, {ov.BaseSpeed}, {ov.BaseSpAttack}, {ov.BaseSpDefense}");
            sb.AppendLine($"    types {ov.Type1}, {ov.Type2}");
            sb.AppendLine($"    catchrate {ov.CatchRate}");
            // BaseExp is managed in data/BaseExperienceTable.c; do not write in mondata.s
            sb.AppendLine($"    evyields {ov.EvYields.hp}, {ov.EvYields.atk}, {ov.EvYields.def}, {ov.EvYields.spd}, {ov.EvYields.spatk}, {ov.EvYields.spdef}");
            sb.AppendLine($"    items {ov.Item1}, {ov.Item2}");
            sb.AppendLine($"    genderratio {ov.GenderRatio}");
            sb.AppendLine($"    eggcycles {ov.EggCycles}");
            sb.AppendLine($"    basefriendship {ov.BaseFriendship}");
            sb.AppendLine($"    growthrate {ov.GrowthRate}");
            sb.AppendLine($"    egggroups {ov.EggGroup1}, {ov.EggGroup2}");
            sb.AppendLine($"    abilities {ov.Ability1}, {ov.Ability2}");
            if (ov.RunChance > 0) sb.AppendLine($"    runchance {ov.RunChance}");
            if (!string.IsNullOrWhiteSpace(ov.DexClassification)) sb.AppendLine($"    mondexclassification {speciesMacro}, \"\"{EscapeQuotes(ov.DexClassification)}\"\"");
            if (!string.IsNullOrWhiteSpace(ov.DexEntry)) sb.AppendLine($"    mondexentry {speciesMacro}, \"\"{EscapeQuotes(ov.DexEntry)}\"\"");
            if (!string.IsNullOrWhiteSpace(ov.DexHeight)) sb.AppendLine($"    mondexheight {speciesMacro}, \"\"{EscapeQuotes(ov.DexHeight)}\"\"");
            if (!string.IsNullOrWhiteSpace(ov.DexWeight)) sb.AppendLine($"    mondexweight {speciesMacro}, \"\"{EscapeQuotes(ov.DexWeight)}\"\"");
            sb.AppendLine();

            var updated = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            return ComputeUnifiedDiff(text, updated, "mondata.s");
        }

        // Save Egg moves for a species in armips/data/eggmoves.s
        public static async Task SaveEggMovesAsync(string speciesMacro, List<string> moves)
        {
            if (ProjectContext.RootPath == null) return;
            var path = Path.Combine(ProjectContext.RootPath, "armips", "data", "eggmoves.s");
            if (!File.Exists(path)) return;

            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"eggmoveentry {speciesMacro}";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return;
            int nextIdx = text.IndexOf("eggmoveentry ", startIdx + blockStart.Length, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;

            var sb = new StringBuilder();
            sb.AppendLine(blockStart);
            foreach (var m in moves)
            {
                sb.AppendLine($"    eggmove {m}");
            }
            sb.AppendLine();

            string newText = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            await File.WriteAllTextAsync(path, newText);
            HGEngineGUI.Services.ChangeLog.Record(path, newText.Length);
        }

        // Save TM/HM list for a species inside armips/data/tmlearnset.txt
        // Strategy: For each TM section, ensure species appears if selected; remove otherwise.
        public static async Task SaveTmHmForSpeciesAsync(string speciesMacro, List<string> selectedTmLabels)
        {
            if (ProjectContext.RootPath == null) return;
            var path = Path.Combine(ProjectContext.RootPath, "armips", "data", "tmlearnset.txt");
            if (!File.Exists(path)) return;

            var lines = (await File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
            // Split into blocks starting with TMxxx:
            var blockRegex = new Regex("^(TM|HM)\\d{3}:.*$", RegexOptions.Multiline);
            var matches = blockRegex.Matches(lines);
            var builder = new StringBuilder();
            int cursor = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                int nextStart = (i + 1 < matches.Count) ? matches[i + 1].Index : lines.Length;
                // Append text before this block unchanged
                builder.Append(lines.Substring(cursor, m.Index - cursor));

                var block = lines.Substring(m.Index, nextStart - m.Index);
                var headerLine = block.Split('\n')[0].Trim(); // e.g., TM001: MOVE_FOCUS_PUNCH

                // Keep species list unique and sorted by appearance; simple approach: rebuild block
                var speciesSet = new HashSet<string>(StringComparer.Ordinal);
                var speciesLineRegex = new Regex(@"^\s*SPECIES_[A-Z0-9_]+\s*$", RegexOptions.Multiline);
                foreach (Match sm in speciesLineRegex.Matches(block))
                {
                    speciesSet.Add(sm.Value.Trim());
                }

                bool shouldHave = selectedTmLabels.Contains(headerLine);
                string speciesLine = "    " + speciesMacro;
                if (shouldHave)
                    speciesSet.Add(speciesLine);
                else
                    speciesSet.Remove(speciesLine);

                // Rebuild block: header + existing species (one per line)
                builder.AppendLine(headerLine);
                foreach (var s in speciesSet.OrderBy(s => s))
                {
                    builder.AppendLine(s);
                }
                builder.AppendLine();
                cursor = nextStart;
            }

            var updated = builder.ToString();
            var backup = path + ".bak";
            try { File.Copy(path, backup, true); } catch { }
            await File.WriteAllTextAsync(path, updated);
        }

        // Save Tutors for a species inside armips/data/tutordata.txt
        // For each TUTOR_* header, species lines are added/removed.
        public static async Task SaveTutorsForSpeciesAsync(string speciesMacro, List<(string tutor, string move, int cost)> selected)
        {
            if (ProjectContext.RootPath == null) return;
            var path = Path.Combine(ProjectContext.RootPath, "armips", "data", "tutordata.txt");
            if (!File.Exists(path)) return;

            var text = (await File.ReadAllTextAsync(path)).Replace("\r\n", "\n");
            var lines = text;
            var tutorHeaderRegex = new Regex(@"^TUTOR_[A-Z0-9_]+:\s+[A-Z0-9_]+\s+\d+\s*$", RegexOptions.Multiline);
            var headers = tutorHeaderRegex.Matches(lines);
            var sb = new StringBuilder();
            int cursor = 0;
            for (int i = 0; i < headers.Count; i++)
            {
                var h = headers[i];
                int nextStart = (i + 1 < headers.Count) ? headers[i + 1].Index : lines.Length;
                sb.Append(lines.Substring(cursor, h.Index - cursor));
                var block = lines.Substring(h.Index, nextStart - h.Index);

                // Does this header appear in selected?
                bool shouldHave = selected.Any(s => block.StartsWith($"{s.tutor}: {s.move} {s.cost}", StringComparison.Ordinal));

                var speciesSet = new HashSet<string>(StringComparer.Ordinal);
                var speciesLineRegex = new Regex(@"^\s*SPECIES_[A-Z0-9_]+\s*$", RegexOptions.Multiline);
                foreach (Match sm in speciesLineRegex.Matches(block)) speciesSet.Add(sm.Value.Trim());

                string speciesLine = "    " + speciesMacro;
                if (shouldHave) speciesSet.Add(speciesLine); else speciesSet.Remove(speciesLine);

                // Rebuild block header line as-is
                var headerLine = block.Split('\n')[0].Trim();
                sb.AppendLine(headerLine);
                foreach (var s in speciesSet.OrderBy(s => s)) sb.AppendLine(s);
                sb.AppendLine();
                cursor = nextStart;
            }
            var updated = sb.ToString();
            var backup = path + ".bak";
            try { File.Copy(path, backup, true); } catch { }
            await File.WriteAllTextAsync(path, updated);
        }

        // Save trainer header for a given trainer id inside armips/data/trainers/trainers.s
        public static async Task SaveTrainerHeaderAsync(int trainerId, string trainerClass, string aiFlags, string battleType, List<string> items)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGParsers.PathTrainers ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            if (!File.Exists(path)) return;

            var text = await File.ReadAllTextAsync(path);
            var headerRegex = new Regex($"trainerdata\\s+{trainerId},\\s*\"(?<name>[^\"]*)\"(?<body>[\\s\\S]*?)endentry", RegexOptions.Multiline);
            var m = headerRegex.Match(text);
            if (!m.Success) return;
            var body = m.Groups["body"].Value;

            // Reconstruct the header body with updated fields, preserving nummons
            var sb = new StringBuilder();
            var nummonsMatch = Regex.Match(body, @"nummons\s+(?<v>\d+)");
            var nummons = nummonsMatch.Success ? nummonsMatch.Groups["v"].Value : "0";
            sb.AppendLine($"\n    trainermontype TRAINER_DATA_TYPE_NOTHING");
            sb.AppendLine($"    trainerclass {trainerClass}");
            sb.AppendLine($"    nummons {nummons}");
            for (int i = 0; i < 4; i++)
            {
                var it = (i < items.Count ? items[i] : "ITEM_NONE");
                sb.AppendLine($"    item {it}");
            }
            sb.AppendLine($"    aiflags {aiFlags}");
            sb.AppendLine($"    battletype {battleType}");
            sb.Append("    endentry\n\n");

            int start = m.Index;
            int end = m.Index + m.Length;
            // Keep the original header line (trainerdata id, "name") and replace only the body
            var headerLineEnd = text.IndexOf('\n', start);
            if (headerLineEnd < 0) headerLineEnd = start;
            string headerLine = text.Substring(start, headerLineEnd - start);
            string newBlock = headerLine + sb.ToString();
            string newText = text.Substring(0, start) + newBlock + text.Substring(end);

            // Backup and write
            var backup = path + ".bak";
            try { File.Copy(path, backup, true); } catch { }
            await File.WriteAllTextAsync(path, newText);
            HGEngineGUI.Services.ChangeLog.Record(path, newText.Length);
        }

        // Save trainer party basics back into party <id> block
        public static async Task SaveTrainerPartyAsync(int trainerId, List<HGEngineGUI.Pages.TrainersPage.PartyRow> rows)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGParsers.PathTrainers ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            if (!File.Exists(path)) return;

            var text = await File.ReadAllTextAsync(path);
            var partyRegex = new Regex($"party\\s+{trainerId}\\s*(?<body>[\\s\\S]*?)endparty", RegexOptions.Multiline);
            var m = partyRegex.Match(text);
            if (!m.Success) return;

            var sb = new StringBuilder();
            sb.AppendLine($"party {trainerId}");
            foreach (var row in rows.OrderBy(r => r.Slot))
            {
                sb.AppendLine($"        // mon {row.Slot}");
                sb.AppendLine($"        ivs {row.IVs}");
                sb.AppendLine($"        abilityslot {row.AbilitySlot}");
                sb.AppendLine($"        level {row.Level}");
                sb.AppendLine($"        pokemon {row.Species}");
                if (!string.IsNullOrWhiteSpace(row.Item)) sb.AppendLine($"        item {row.Item}");
                if (!string.IsNullOrWhiteSpace(row.Move1)) sb.AppendLine($"        move {row.Move1}");
                if (!string.IsNullOrWhiteSpace(row.Move2)) sb.AppendLine($"        move {row.Move2}");
                if (!string.IsNullOrWhiteSpace(row.Move3)) sb.AppendLine($"        move {row.Move3}");
                if (!string.IsNullOrWhiteSpace(row.Move4)) sb.AppendLine($"        move {row.Move4}");
                if (!string.IsNullOrWhiteSpace(row.Nature)) sb.AppendLine($"        nature {row.Nature}");
                if (row.Form != 0) sb.AppendLine($"        form {row.Form}");
                if (!string.IsNullOrWhiteSpace(row.Ball)) sb.AppendLine($"        ball {row.Ball}");
                if (row.ShinyLock) sb.AppendLine($"        shinylock 1");
                if (!string.IsNullOrWhiteSpace(row.Nickname)) sb.AppendLine($"        nickname \"{row.Nickname}\"");
                if (!string.IsNullOrWhiteSpace(row.PP)) sb.AppendLine($"        pp {row.PP}");
                sb.AppendLine($"        ballseal 0");
                sb.AppendLine();
            }
            sb.AppendLine("    endparty");

            int start = m.Index;
            int end = m.Index + m.Length;
            string newText = text.Substring(0, start) + sb.ToString() + text.Substring(end);
            var backup = path + ".bak";
            try { File.Copy(path, backup, true); } catch { }
            await File.WriteAllTextAsync(path, newText);
        }

        // Save evolutions for a species into armips/data/evodata.s
        public static async Task SaveEvolutionsAsync(string speciesMacro, List<(string method, int param, string target)> evolutions)
        {
            if (ProjectContext.RootPath == null) return;
            var path = Path.Combine(ProjectContext.RootPath, "armips", "data", "evodata.s");
            if (!File.Exists(path)) return;

            var text = await File.ReadAllTextAsync(path);
            var blockStart = $"evodata {speciesMacro}";
            int startIdx = text.IndexOf(blockStart, StringComparison.Ordinal);
            if (startIdx < 0) return;
            // Replace entire evodata block up to next header
            int nextIdx = text.IndexOf("\n\nevodata ", startIdx + blockStart.Length, StringComparison.Ordinal);
            if (nextIdx < 0) nextIdx = text.IndexOf("evodata ", startIdx + blockStart.Length, StringComparison.Ordinal);
            int endIdx = nextIdx >= 0 ? nextIdx : text.Length;

            var sb = new StringBuilder();
            sb.AppendLine(blockStart);
            foreach (var (method, param, target) in evolutions)
            {
                sb.AppendLine($"    evolution {method}, {param}, {target}");
            }
            // Fill remaining slots to 9 with EVO_NONE for structure consistency (optional)
            int count = evolutions.Count;
            while (count++ < 9) sb.AppendLine("    evolution EVO_NONE, 0, SPECIES_NONE");
            sb.AppendLine("    terminateevodata");
            sb.AppendLine();

            var original = text;
            string newText = text.Substring(0, startIdx) + sb.ToString() + text.Substring(endIdx);
            // Show diff via consumer preview if needed; here we just back up and write
            var backup = path + ".bak";
            try { File.Copy(path, backup, true); } catch { }
            await File.WriteAllTextAsync(path, newText);
        }
    }
}


