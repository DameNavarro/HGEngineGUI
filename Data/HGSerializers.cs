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

        // Items: preview/save price changes into data/itemdata/itemdata.c
        public static async Task<string> PreviewItemPricesAsync(List<(string ItemMacro, int Price)> entries)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathItemData ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "itemdata", "itemdata.c");
            if (!System.IO.File.Exists(path)) return string.Empty;
            var original = await System.IO.File.ReadAllTextAsync(path);
            var updated = original;
            foreach (var (item, price) in entries)
            {
                var rx = new System.Text.RegularExpressions.Regex(@"(\[\s*" + System.Text.RegularExpressions.Regex.Escape(item) + @"\s*\]\s*=\s*\{[\s\S]*?\.price\s*=\s*)(\d+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                updated = rx.Replace(updated, m => m.Groups[1].Value + price.ToString(), 1);
            }
            return ComputeUnifiedDiff(original, updated, "itemdata.c");
        }

        public static async Task SaveItemPricesAsync(List<(string ItemMacro, int Price)> entries)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGParsers.PathItemData ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "itemdata", "itemdata.c");
            if (!System.IO.File.Exists(path)) return;
            var text = await System.IO.File.ReadAllTextAsync(path);
            foreach (var (item, price) in entries)
            {
                var rx = new System.Text.RegularExpressions.Regex(@"(\[\s*" + System.Text.RegularExpressions.Regex.Escape(item) + @"\s*\]\s*=\s*\{[\s\S]*?\.price\s*=\s*)(\d+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                text = rx.Replace(text, m => m.Groups[1].Value + price.ToString(), 1);
            }
            var backup = path + ".bak";
            try { System.IO.File.Copy(path, backup, true); } catch { }
            await System.IO.File.WriteAllTextAsync(path, text);
            HGEngineGUI.Services.ChangeLog.Record(path, text.Length);
        }

        // Preview/save updates to multiple item fields inside data/itemdata/itemdata.c
        public static async Task<string> PreviewItemDataAsync(List<HGParsers.ItemDataEntry> entries)
        {
            if (ProjectContext.RootPath == null || entries == null || entries.Count == 0) return string.Empty;
            var path = HGParsers.PathItemData ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "itemdata", "itemdata.c");
            if (!System.IO.File.Exists(path)) return string.Empty;
            var original = await System.IO.File.ReadAllTextAsync(path);
            var updated = await ReplaceItemDataAsync(original, entries);
            return ComputeUnifiedDiff(original, updated, "itemdata.c");
        }

        public static async Task SaveItemDataAsync(List<HGParsers.ItemDataEntry> entries)
        {
            if (ProjectContext.RootPath == null || entries == null || entries.Count == 0) return;
            var path = HGParsers.PathItemData ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "itemdata", "itemdata.c");
            if (!System.IO.File.Exists(path)) return;
            var text = await System.IO.File.ReadAllTextAsync(path);
            var updated = await ReplaceItemDataAsync(text, entries);
            var backup = path + ".bak";
            try { System.IO.File.Copy(path, backup, true); } catch { }
            await System.IO.File.WriteAllTextAsync(path, updated);
            HGEngineGUI.Services.ChangeLog.Record(path, updated.Length);
        }

        private static Task<string> ReplaceItemDataAsync(string text, List<HGParsers.ItemDataEntry> entries)
        {
            // Normalize newlines for consistency
            bool hadCrLf = text.Contains("\r\n");
            string normalized = hadCrLf ? text.Replace("\r\n", "\n") : text;

            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.ItemMacro)) continue;
                // Match one item block by macro, tolerant of index arithmetic (e.g., - NUM_UNKNOWN_SLOTS)
                var blockRx = new System.Text.RegularExpressions.Regex("\\[\\s*" + System.Text.RegularExpressions.Regex.Escape(e.ItemMacro) + @"(?:\\s*-\\s*NUM_UNKNOWN_SLOTS(?:_EXPLORER_KIT)?)?\\s*\\]\\s*=\\s*\\{(?<body>[\\s\\S]*?)\\}", System.Text.RegularExpressions.RegexOptions.Multiline);
                var m = blockRx.Match(normalized);
                if (!m.Success) continue;
                int start = m.Index;
                int length = m.Length;
                string fullBlock = normalized.Substring(start, length);
                string body = m.Groups["body"].Value;

                // Update top-level assignments
                string newBody = ReplaceAssignment(body, "price", e.Price);
                newBody = ReplaceAssignment(newBody, "holdEffect", e.HoldEffect);
                newBody = ReplaceAssignment(newBody, "holdEffectParam", e.HoldEffectParam);
                newBody = ReplaceAssignment(newBody, "pluckEffect", e.PluckEffect);
                newBody = ReplaceAssignment(newBody, "flingEffect", e.FlingEffect);
                newBody = ReplaceAssignment(newBody, "flingPower", e.FlingPower);
                newBody = ReplaceAssignment(newBody, "naturalGiftPower", e.NaturalGiftPower);
                if (!string.IsNullOrWhiteSpace(e.NaturalGiftType)) newBody = ReplaceAssignment(newBody, "naturalGiftType", e.NaturalGiftType);
                newBody = ReplaceAssignment(newBody, "prevent_toss", e.PreventToss);
                newBody = ReplaceAssignment(newBody, "selectable", e.Selectable);
                if (!string.IsNullOrWhiteSpace(e.FieldPocket)) newBody = ReplaceAssignment(newBody, "fieldPocket", e.FieldPocket);
                if (!string.IsNullOrWhiteSpace(e.BattlePocket)) newBody = ReplaceAssignment(newBody, "battlePocket", e.BattlePocket);
                newBody = ReplaceAssignment(newBody, "fieldUseFunc", e.FieldUseFunc);
                newBody = ReplaceAssignment(newBody, "battleUseFunc", e.BattleUseFunc);
                newBody = ReplaceAssignment(newBody, "partyUse", e.PartyUse);

                // Update nested partyUseParam block
                var partyRx = new System.Text.RegularExpressions.Regex(@"\.partyUseParam\s*=\s*\{(?<p>[\s\S]*?)\}", System.Text.RegularExpressions.RegexOptions.Multiline);
                var pm = partyRx.Match(newBody);
                if (pm.Success)
                {
                    string pbody = pm.Groups["p"].Value;
                    foreach (var kv in e.PartyFlags)
                    {
                        pbody = ReplaceAssignment(pbody, kv.Key, kv.Value);
                    }
                    foreach (var kv in e.PartyParams)
                    {
                        pbody = ReplaceAssignment(pbody, kv.Key, kv.Value);
                    }
                    // splice back
                    newBody = newBody.Substring(0, pm.Groups["p"].Index) + pbody + newBody.Substring(pm.Groups["p"].Index + pm.Groups["p"].Length);
                }
                else if ((e.PartyFlags.Count + e.PartyParams.Count) > 0)
                {
                    // Insert minimal partyUseParam block before closing brace
                    var sb = new System.Text.StringBuilder();
                    sb.Append("\n        .partyUseParam = {\n");
                    foreach (var kv in e.PartyFlags) sb.Append($"            .{kv.Key} = {kv.Value},\n");
                    foreach (var kv in e.PartyParams) sb.Append($"            .{kv.Key} = {kv.Value},\n");
                    sb.Append("        },\n");
                    // inject at end of body
                    newBody = newBody.TrimEnd();
                    newBody += sb.ToString();
                }

                // Reassemble the block
                string newBlock = fullBlock.Replace(m.Groups["body"].Value, newBody);
                // Replace in the full text
                normalized = normalized.Substring(0, start) + newBlock + normalized.Substring(start + length);
            }

            string result = hadCrLf ? normalized.Replace("\n", "\r\n") : normalized;
            return Task.FromResult(result);
        }

        private static string ReplaceAssignment(string body, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return body;
            var rx = new System.Text.RegularExpressions.Regex(@"(\." + System.Text.RegularExpressions.Regex.Escape(field) + @"\s*=\s*)([^,\}\n]+)", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (rx.IsMatch(body))
            {
                return rx.Replace(body, m => m.Groups[1].Value + value, 1);
            }
            // If field not found, append it near end (best-effort)
            int insertPos = body.LastIndexOf('}');
            if (insertPos < 0) insertPos = body.Length;
            var line = "\n        ." + field + " = " + value + ",\n";
            return body.Insert(insertPos, line);
        }

        // Preview/save item names and descriptions in data/text/222.txt and 221.txt
        public static async Task<string> PreviewItemTextAsync(List<(int ItemId, string? Name, string? Description)> changes)
        {
            if (ProjectContext.RootPath == null || changes == null || changes.Count == 0) return string.Empty;
            string? namesPath = HGParsers.PathItemNames ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "text", "222.txt");
            string? descPath = HGParsers.PathItemDescriptions ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "text", "221.txt");
            var diffs = new System.Text.StringBuilder();

            if (namesPath != null && System.IO.File.Exists(namesPath))
            {
                var original = await System.IO.File.ReadAllTextAsync(namesPath);
                var updated = await ApplyTextChangesAsync(original, changes, isNames: true);
                diffs.AppendLine(ComputeUnifiedDiff(original, updated, "222.txt"));
            }
            if (descPath != null && System.IO.File.Exists(descPath))
            {
                var original = await System.IO.File.ReadAllTextAsync(descPath);
                var updated = await ApplyTextChangesAsync(original, changes, isNames: false);
                diffs.AppendLine(ComputeUnifiedDiff(original, updated, "221.txt"));
            }
            return diffs.ToString();
        }

        public static async Task SaveItemTextAsync(List<(int ItemId, string? Name, string? Description)> changes)
        {
            if (ProjectContext.RootPath == null || changes == null || changes.Count == 0) return;
            string? namesPath = HGParsers.PathItemNames ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "text", "222.txt");
            string? descPath = HGParsers.PathItemDescriptions ?? System.IO.Path.Combine(ProjectContext.RootPath, "data", "text", "221.txt");

            if (namesPath != null && System.IO.File.Exists(namesPath))
            {
                var original = await System.IO.File.ReadAllTextAsync(namesPath);
                var updated = await ApplyTextChangesAsync(original, changes, isNames: true);
                var backup = namesPath + ".bak";
                try { System.IO.File.Copy(namesPath, backup, true); } catch { }
                await System.IO.File.WriteAllTextAsync(namesPath, updated);
                HGEngineGUI.Services.ChangeLog.Record(namesPath, updated.Length);
            }
            if (descPath != null && System.IO.File.Exists(descPath))
            {
                var original = await System.IO.File.ReadAllTextAsync(descPath);
                var updated = await ApplyTextChangesAsync(original, changes, isNames: false);
                var backup = descPath + ".bak";
                try { System.IO.File.Copy(descPath, backup, true); } catch { }
                await System.IO.File.WriteAllTextAsync(descPath, updated);
                HGEngineGUI.Services.ChangeLog.Record(descPath, updated.Length);
            }
        }

        private static Task<string> ApplyTextChangesAsync(string original, List<(int ItemId, string? Name, string? Description)> changes, bool isNames)
        {
            bool hadCrLf = original.Contains("\r\n");
            var lines = (hadCrLf ? original.Replace("\r\n", "\n") : original).Split('\n').ToList();
            int maxIndex = lines.Count - 1;
            foreach (var ch in changes)
            {
                int idx = ch.ItemId; // mapping is 0-based id -> line index
                if (idx < 0) continue;
                if (idx > maxIndex)
                {
                    // grow with placeholders
                    while (maxIndex < idx)
                    {
                        lines.Add(isNames ? string.Empty : "-----");
                        maxIndex++;
                    }
                }
                if (isNames)
                {
                    if (ch.Name != null) lines[idx] = ch.Name.Replace("\r\n", "\n");
                }
                else
                {
                    if (ch.Description != null) lines[idx] = ch.Description.Replace("\r\n", "\n");
                }
            }
            string joined = string.Join("\n", lines);
            return Task.FromResult(hadCrLf ? joined.Replace("\n", "\r\n") : joined);
        }

        // Mart items: preview/save by reconstructing .halfword sequences inside armips/asm/custom/mart_items.s
        public static async Task<string> PreviewMartItemsAsync(List<HGEngineGUI.Data.HGParsers.MartSection> sections)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGEngineGUI.Data.HGParsers.PathMartItems ?? System.IO.Path.Combine(ProjectContext.RootPath, "armips", "asm", "custom", "mart_items.s");
            if (!System.IO.File.Exists(path)) return string.Empty;
            var original = await System.IO.File.ReadAllTextAsync(path);
            string updated = ReplaceMartSections(original, sections);
            return ComputeUnifiedDiff(original, updated, "mart_items.s");
        }

        public static async Task SaveMartItemsAsync(List<HGEngineGUI.Data.HGParsers.MartSection> sections)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGEngineGUI.Data.HGParsers.PathMartItems ?? System.IO.Path.Combine(ProjectContext.RootPath, "armips", "asm", "custom", "mart_items.s");
            if (!System.IO.File.Exists(path)) return;
            var text = await System.IO.File.ReadAllTextAsync(path);
            var updated = ReplaceMartSections(text, sections);
            var backup = path + ".bak";
            try { System.IO.File.Copy(path, backup, true); } catch { }
            await System.IO.File.WriteAllTextAsync(path, updated);
            HGEngineGUI.Services.ChangeLog.Record(path, updated.Length);
        }

        private static string ReplaceMartSections(string text, List<HGEngineGUI.Data.HGParsers.MartSection> sections)
        {
            // Normalize line endings for robust matching
            bool hadCrLf = text.Contains("\r\n");
            string normalized = hadCrLf ? text.Replace("\r\n", "\n") : text;
            var lines = normalized.Split('\n').ToList();

            foreach (var sec in sections)
            {
                // Locate all header lines for this section: ".org <address>" (handle prior duplicates)
                var headerIndices = new List<int>();
                for (int i = 0; i < lines.Count; i++)
                {
                    var ltrim = lines[i].TrimStart();
                    if (!ltrim.StartsWith(".org ", StringComparison.OrdinalIgnoreCase)) continue;
                    var afterOrg = ltrim.Substring(5).Trim();
                    if (afterOrg.StartsWith(sec.Key, StringComparison.OrdinalIgnoreCase)) headerIndices.Add(i);
                }
                if (headerIndices.Count == 0) continue; // only replace existing, never append
                int headerIdx = headerIndices[0];

                // Determine the inventory body as contiguous .halfword lines (allowing blank lines) after header
                int bodyStart = headerIdx + 1;
                // keep existing blank line immediately after header as part of body range
                int bodyEndExclusive = bodyStart;
                while (bodyEndExclusive < lines.Count)
                {
                    var trim = lines[bodyEndExclusive].TrimStart();
                    if (trim.Length == 0 || trim.StartsWith(".halfword ", StringComparison.OrdinalIgnoreCase))
                    {
                        bodyEndExclusive++;
                        continue;
                    }
                    break; // stop at first non-blank, non-.halfword line to preserve comments and next sections
                }

                // Build new body (preserve one blank line after header if originally present)
                bool hadBlankAfterHeader = (bodyStart < lines.Count) && lines[bodyStart].Trim().Length == 0;
                var newBody = new List<string>();
                if (hadBlankAfterHeader) newBody.Add(string.Empty);
                if (sec.IsGeneralTable)
                {
                    foreach (var it in sec.Items)
                    {
                        var badge = string.IsNullOrWhiteSpace(it.BadgeMacro) ? "ZERO_BADGES" : it.BadgeMacro;
                        newBody.Add($".halfword {it.Item}");
                        newBody.Add($".halfword {badge}");
                        newBody.Add(string.Empty);
                    }
                    // Trim possible trailing extra blank line to match style
                    while (newBody.Count > 0 && newBody[^1].Length == 0) newBody.RemoveAt(newBody.Count - 1);
                }
                else
                {
                    foreach (var it in sec.Items)
                        newBody.Add($".halfword {it.Item}");
                    newBody.Add($".halfword 0xFFFF");
                }

                // Replace in place: keep header line and everything else; swap the body range
                int removeCount = Math.Max(0, bodyEndExclusive - bodyStart);
                lines.RemoveRange(bodyStart, removeCount);
                lines.InsertRange(bodyStart, newBody);

                // Remove any subsequent duplicate blocks for the same address entirely
                // Scan indices again since list changed length
                for (int k = lines.Count - 1; k >= 0; k--)
                {
                    var ltrim = lines[k].TrimStart();
                    if (!ltrim.StartsWith(".org ", StringComparison.OrdinalIgnoreCase)) continue;
                    var afterOrg = ltrim.Substring(5).Trim();
                    if (!afterOrg.StartsWith(sec.Key, StringComparison.OrdinalIgnoreCase)) continue;
                    if (k == headerIdx) continue; // keep the first (edited) block

                    int dupStart = k;
                    int dupEndExclusive = dupStart + 1;
                    while (dupEndExclusive < lines.Count)
                    {
                        var tline = lines[dupEndExclusive].TrimStart();
                        if (tline.StartsWith(".org ", StringComparison.OrdinalIgnoreCase) || tline.StartsWith(".close", StringComparison.OrdinalIgnoreCase)) break;
                        dupEndExclusive++;
                    }
                    lines.RemoveRange(dupStart, dupEndExclusive - dupStart);
                }
            }

            string result = string.Join("\n", lines);
            return hadCrLf ? result.Replace("\n", "\r\n") : result;
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

        // Accept expanded UI representation in future: one row per evolution line that internally may include multiple method conditions.
        public static async Task<string> PreviewEvolutionsAsync(string speciesMacro, List<(string method, int param, string target, int form)> evolutions)
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
            foreach (var (method, param, target, form) in evolutions)
            {
                var paramToken = param.ToString();
                // Replace param with macro tokens when applicable
                if (method == "EVO_ITEM" || method == "EVO_TRADE_ITEM" || method == "EVO_STONE" || method == "EVO_STONE_MALE" || method == "EVO_STONE_FEMALE" || method == "EVO_ITEM_DAY" || method == "EVO_ITEM_NIGHT")
                {
                    if (HGParsers.TryGetItemMacro(param, out var mac)) paramToken = mac;
                }
                else if (method == "EVO_HAS_MOVE")
                {
                    if (HGParsers.TryGetMoveMacro(param, out var mac)) paramToken = mac;
                }
                else if (method == "EVO_HAS_MOVE_TYPE")
                {
                    if (HGParsers.TryGetTypeMacro(param, out var mac)) paramToken = mac;
                }
                else if (method == "EVO_OTHER_PARTY_MON" || method == "EVO_TRADE_SPECIFIC_MON")
                {
                    if (HGParsers.TryGetSpeciesMacro(param, out var mac)) paramToken = mac;
                }

                if (form > 0)
                    sb.AppendLine($"    evolution {method}, {paramToken}, {target} | ({form} << 11)");
                else
                    sb.AppendLine($"    evolution {method}, {paramToken}, {target}");
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

        // Base Experience table (data/BaseExperienceTable.c)
        public static async Task<string> PreviewBaseExpAsync(string speciesMacro, int baseExp)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = Path.Combine(ProjectContext.RootPath, "data", "BaseExperienceTable.c");
            if (!File.Exists(path)) return string.Empty;
            var original = await File.ReadAllTextAsync(path);
            var pattern = new Regex(@"(\[\s*" + Regex.Escape(speciesMacro) + @"\s*\]\s*=\s*)(\d+)(\s*,)", RegexOptions.Multiline);
            if (!pattern.IsMatch(original)) return string.Empty;
            var updated = pattern.Replace(original, m => m.Groups[1].Value + baseExp.ToString() + m.Groups[3].Value, 1);
            return ComputeUnifiedDiff(original, updated, "BaseExperienceTable.c");
        }

        public static async Task SaveBaseExpAsync(string speciesMacro, int baseExp)
        {
            if (ProjectContext.RootPath == null) return;
            var path = Path.Combine(ProjectContext.RootPath, "data", "BaseExperienceTable.c");
            if (!File.Exists(path)) return;
            var text = await File.ReadAllTextAsync(path);
            var pattern = new Regex(@"(\[\s*" + Regex.Escape(speciesMacro) + @"\s*\]\s*=\s*)(\d+)(\s*,)", RegexOptions.Multiline);
            if (!pattern.IsMatch(text)) return;
            var updated = pattern.Replace(text, m => m.Groups[1].Value + baseExp.ToString() + m.Groups[3].Value, 1);
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

            // Extract existing header name and any lines we don't currently manage (e.g., baseexp, colorflip)
            string existingBlock = text.Substring(startIdx, endIdx - startIdx);
            string headerLine = existingBlock.Split('\n').FirstOrDefault() ?? string.Empty;
            var headerNameMatch = new System.Text.RegularExpressions.Regex(@"mondata\s+" + System.Text.RegularExpressions.Regex.Escape(speciesMacro) + @",\s*""(?<name>[^""]*)""").Match(headerLine);
            string displayName = headerNameMatch.Success ? headerNameMatch.Groups["name"].Value : string.Empty;
            // Preserve baseexp line (commented in this repo) and colorflip if present
            string preservedBaseExp = string.Empty;
            string preservedColorFlip = string.Empty;
            foreach (var line in existingBlock.Replace("\r\n", "\n").Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("baseexp ", StringComparison.Ordinal)) preservedBaseExp = line;
                if (trimmed.StartsWith("colorflip ", StringComparison.Ordinal)) preservedColorFlip = line;
            }

            var sb = new StringBuilder();
            sb.AppendLine(blockStart + $" \"{EscapeQuotes(displayName)}\"");
            sb.AppendLine($"    basestats {ov.BaseHp}, {ov.BaseAttack}, {ov.BaseDefense}, {ov.BaseSpeed}, {ov.BaseSpAttack}, {ov.BaseSpDefense}");
            sb.AppendLine($"    types {ov.Type1}, {ov.Type2}");
            sb.AppendLine($"    catchrate {ov.CatchRate}");
            // Preserve optional lines we don't edit (baseexp comment) in canonical order under catchrate
            if (!string.IsNullOrWhiteSpace(preservedBaseExp)) sb.AppendLine(preservedBaseExp);
            // BaseExp is managed in data/BaseExperienceTable.c; actual value written there via SaveBaseExpAsync
            sb.AppendLine($"    evyields {ov.EvYields.hp}, {ov.EvYields.atk}, {ov.EvYields.def}, {ov.EvYields.spd}, {ov.EvYields.spatk}, {ov.EvYields.spdef}");
            sb.AppendLine($"    items {ov.Item1}, {ov.Item2}");
            sb.AppendLine($"    genderratio {ov.GenderRatio}");
            sb.AppendLine($"    eggcycles {ov.EggCycles}");
            sb.AppendLine($"    basefriendship {ov.BaseFriendship}");
            sb.AppendLine($"    growthrate {ov.GrowthRate}");
            sb.AppendLine($"    egggroups {ov.EggGroup1}, {ov.EggGroup2}");
            sb.AppendLine($"    abilities {ov.Ability1}, {ov.Ability2}");
            // Always write runchance, even if zero, to mirror original formatting
            sb.AppendLine($"    runchance {ov.RunChance}");
            // Hidden ability is stored in data/HiddenAbilityTable.c and not in mondata.s
            // Preserve optional lines we don't edit (colorflip)
            if (!string.IsNullOrWhiteSpace(preservedColorFlip)) sb.AppendLine(preservedColorFlip);
            if (!string.IsNullOrWhiteSpace(ov.DexClassification)) sb.AppendLine($"    mondexclassification {speciesMacro}, \"{EscapeQuotes(ov.DexClassification)}\"");
            if (!string.IsNullOrWhiteSpace(ov.DexEntry)) sb.AppendLine($"    mondexentry {speciesMacro}, \"{EscapeQuotes(ov.DexEntry)}\"");
            if (!string.IsNullOrWhiteSpace(ov.DexHeight)) sb.AppendLine($"    mondexheight {speciesMacro}, \"{EscapeQuotes(ov.DexHeight)}\"");
            if (!string.IsNullOrWhiteSpace(ov.DexWeight)) sb.AppendLine($"    mondexweight {speciesMacro}, \"{EscapeQuotes(ov.DexWeight)}\"");
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
            // Read existing header name and preserved lines for preview as well
            string existingBlock = text.Substring(startIdx, endIdx - startIdx);
            string headerLine = existingBlock.Split('\n').FirstOrDefault() ?? string.Empty;
            var headerNameMatch = new System.Text.RegularExpressions.Regex(@"mondata\s+" + System.Text.RegularExpressions.Regex.Escape(speciesMacro) + @",\s*""(?<name>[^""]*)""").Match(headerLine);
            string displayName = headerNameMatch.Success ? headerNameMatch.Groups["name"].Value : string.Empty;
            string preservedBaseExp = string.Empty;
            string preservedColorFlip = string.Empty;
            foreach (var line in existingBlock.Replace("\r\n", "\n").Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("baseexp ", StringComparison.Ordinal)) preservedBaseExp = line;
                if (trimmed.StartsWith("colorflip ", StringComparison.Ordinal)) preservedColorFlip = line;
            }

            sb.AppendLine(blockStart + $" \"{EscapeQuotes(displayName)}\"");
            sb.AppendLine($"    basestats {ov.BaseHp}, {ov.BaseAttack}, {ov.BaseDefense}, {ov.BaseSpeed}, {ov.BaseSpAttack}, {ov.BaseSpDefense}");
            sb.AppendLine($"    types {ov.Type1}, {ov.Type2}");
            sb.AppendLine($"    catchrate {ov.CatchRate}");
            // Preserve baseexp comment in canonical order
            if (!string.IsNullOrWhiteSpace(preservedBaseExp)) sb.AppendLine(preservedBaseExp);
            // BaseExp actual value preview handled via PreviewBaseExpAsync if needed
            sb.AppendLine($"    evyields {ov.EvYields.hp}, {ov.EvYields.atk}, {ov.EvYields.def}, {ov.EvYields.spd}, {ov.EvYields.spatk}, {ov.EvYields.spdef}");
            sb.AppendLine($"    items {ov.Item1}, {ov.Item2}");
            sb.AppendLine($"    genderratio {ov.GenderRatio}");
            sb.AppendLine($"    eggcycles {ov.EggCycles}");
            sb.AppendLine($"    basefriendship {ov.BaseFriendship}");
            sb.AppendLine($"    growthrate {ov.GrowthRate}");
            sb.AppendLine($"    egggroups {ov.EggGroup1}, {ov.EggGroup2}");
            sb.AppendLine($"    abilities {ov.Ability1}, {ov.Ability2}");
            sb.AppendLine($"    runchance {ov.RunChance}");
            if (!string.IsNullOrWhiteSpace(preservedColorFlip)) sb.AppendLine(preservedColorFlip);
            if (!string.IsNullOrWhiteSpace(ov.DexClassification)) sb.AppendLine($"    mondexclassification {speciesMacro}, \"{EscapeQuotes(ov.DexClassification)}\"");
            if (!string.IsNullOrWhiteSpace(ov.DexEntry)) sb.AppendLine($"    mondexentry {speciesMacro}, \"{EscapeQuotes(ov.DexEntry)}\"");
            if (!string.IsNullOrWhiteSpace(ov.DexHeight)) sb.AppendLine($"    mondexheight {speciesMacro}, \"{EscapeQuotes(ov.DexHeight)}\"");
            if (!string.IsNullOrWhiteSpace(ov.DexWeight)) sb.AppendLine($"    mondexweight {speciesMacro}, \"{EscapeQuotes(ov.DexWeight)}\"");
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

        // Encounters: save one encounter area block back into armips/data/encounters.s
        public static async Task<string> PreviewEncounterAreaAsync(HGParsers.EncounterArea area)
        {
            if (ProjectContext.RootPath == null) return string.Empty;
            var path = HGParsers.PathEncounters ?? System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "encounters.s");
            if (!System.IO.File.Exists(path)) return string.Empty;
            var original = await System.IO.File.ReadAllTextAsync(path);
            string updated = ReplaceEncounterBlock(original, area);
            return ComputeUnifiedDiff(original, updated, "encounters.s");
        }

        public static async Task SaveEncounterAreaAsync(HGParsers.EncounterArea area)
        {
            if (ProjectContext.RootPath == null) return;
            var path = HGParsers.PathEncounters ?? System.IO.Path.Combine(ProjectContext.RootPath, "armips", "data", "encounters.s");
            if (!System.IO.File.Exists(path)) return;
            var text = await System.IO.File.ReadAllTextAsync(path);
            var updated = ReplaceEncounterBlock(text, area);
            var backup = path + ".bak";
            try { System.IO.File.Copy(path, backup, true); } catch { }
            await System.IO.File.WriteAllTextAsync(path, updated);
            HGEngineGUI.Services.ChangeLog.Record(path, updated.Length);
        }

        private static string ReplaceEncounterBlock(string text, HGParsers.EncounterArea area)
        {
            var regex = new System.Text.RegularExpressions.Regex(@"encounterdata\s+" + area.Id + @"\s*//[\s\S]*?\.close", System.Text.RegularExpressions.RegexOptions.Multiline);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"encounterdata   {area.Id}   // {area.Label}");
            sb.AppendLine();
            sb.AppendLine($"walkrate {area.WalkRate}");
            sb.AppendLine($"surfrate {area.SurfRate}");
            sb.AppendLine($"rocksmashrate {area.RockSmashRate}");
            sb.AppendLine($"oldrodrate {area.OldRodRate}");
            sb.AppendLine($"goodrodrate {area.GoodRodRate}");
            sb.AppendLine($"superrodrate {area.SuperRodRate}");
            sb.AppendLine($"walklevels {string.Join(", ", area.WalkLevels.Select(v => v.ToString()))}");
            sb.AppendLine("// walklevels specifies the levels of each slot.  each slot gets its own individual level without a range, which is different compared to the encounter format of later entries.");
            sb.AppendLine("// replace \"pokemon SPECIES_*\" with \"monwithform SPECIES_*, formid\" to get the specific form of a pokemon.  if i want a galarian darumaka, i'd put \"monwithform SPECIES_DARUMAKA, 1\"");
            sb.AppendLine("// probabilities:  " + string.Join(", ", area.GrassProbabilities));
            sb.AppendLine();

            void WriteSpeciesList(string header, IEnumerable<string> list)
            {
                sb.AppendLine($"// {header}");
                foreach (var s in list) sb.AppendLine($"pokemon {s}");
                sb.AppendLine();
            }
            WriteSpeciesList("morning encounter slots", area.MorningGrass);
            WriteSpeciesList("day encounter slots", area.DayGrass);
            WriteSpeciesList("night encounter slots", area.NightGrass);
            WriteSpeciesList("hoenn encounter slots", area.HoennGrass);
            WriteSpeciesList("sinnoh encounter slots", area.SinnohGrass);

            void WriteEncounters(string header, IEnumerable<HGParsers.EncounterSlot> list, int[] probs)
            {
                sb.AppendLine($"// {header}");
                if (probs.Length > 0) sb.AppendLine($"// probabilities:  {string.Join(", ", probs)}");
                foreach (var e in list) sb.AppendLine($"encounter {e.SpeciesMacro}, {e.MinLevel}, {e.MaxLevel}");
                sb.AppendLine();
            }
            WriteEncounters("surf encounters", area.Surf, area.SurfProbabilities);
            WriteEncounters("rock smash encounters", area.RockSmash, area.RockProbabilities);
            WriteEncounters("old rod encounters", area.OldRod, area.OldRodProbabilities);
            WriteEncounters("good rod encounters", area.GoodRod, area.GoodRodProbabilities);
            WriteEncounters("super rod encounters", area.SuperRod, area.SuperRodProbabilities);

            sb.AppendLine("// swarm grass"); sb.AppendLine($"pokemon {area.SwarmGrass}");
            sb.AppendLine("// swarm surf"); sb.AppendLine($"pokemon {area.SwarmSurf}");
            sb.AppendLine("// swarm good rod"); sb.AppendLine($"pokemon {area.SwarmGoodRod}");
            sb.AppendLine("// swarm super rod"); sb.AppendLine($"pokemon {area.SwarmSuperRod}");
            sb.AppendLine();
            sb.AppendLine(".close");

            if (regex.IsMatch(text))
            {
                return regex.Replace(text, sb.ToString(), 1);
            }
            // Fallback: append at end
            return text.TrimEnd() + "\n\n" + sb.ToString() + "\n";
        }

        // Save trainer header for a given trainer id inside armips/data/trainers/trainers.s
        public static async Task SaveTrainerHeaderAsync(int trainerId, string trainerClass, string aiFlags, string battleType, List<string> items, string trainerDataTypeFlags)
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
            sb.AppendLine($"\n    trainermontype {trainerDataTypeFlags}");
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
                // Additional flags and fields
                int af = row.AdditionalFlags;
                if (af == 0)
                {
                    if (row.Status != 0) af |= 0x01;
                    if (row.Hp != 0) af |= 0x02;
                    if (row.Atk != 0) af |= 0x04;
                    if (row.Def != 0) af |= 0x08;
                    if (row.Speed != 0) af |= 0x10;
                    if (row.SpAtk != 0) af |= 0x20;
                    if (row.SpDef != 0) af |= 0x40;
                    if (!string.IsNullOrWhiteSpace(row.PPCounts)) af |= 0x80;
                    if (!string.IsNullOrWhiteSpace(row.Nickname)) af |= 0x100;
                }
                if (af != 0) sb.AppendLine($"        additionalflags 0x{af:X}");
                if (row.Status != 0) sb.AppendLine($"        status {row.Status}");
                if (row.Hp != 0) sb.AppendLine($"        stathp {row.Hp}");
                if (row.Atk != 0) sb.AppendLine($"        statatk {row.Atk}");
                if (row.Def != 0) sb.AppendLine($"        statdef {row.Def}");
                if (row.Speed != 0) sb.AppendLine($"        statspeed {row.Speed}");
                if (row.SpAtk != 0) sb.AppendLine($"        statspatk {row.SpAtk}");
                if (row.SpDef != 0) sb.AppendLine($"        statspdef {row.SpDef}");
                if (!string.IsNullOrWhiteSpace(row.Type1) || !string.IsNullOrWhiteSpace(row.Type2))
                {
                    var t1 = string.IsNullOrWhiteSpace(row.Type1) ? "TYPE_NORMAL" : row.Type1;
                    var t2 = string.IsNullOrWhiteSpace(row.Type2) ? t1 : row.Type2;
                    sb.AppendLine($"        types {t1}, {t2}");
                }
                if (!string.IsNullOrWhiteSpace(row.PPCounts)) sb.AppendLine($"        ppcounts {row.PPCounts}");
                if (!string.IsNullOrWhiteSpace(row.Nickname)) sb.AppendLine($"        nickname \"{row.Nickname}\"");
                sb.AppendLine($"        ballseal {row.BallSeal}");
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
        public static async Task SaveEvolutionsAsync(string speciesMacro, List<(string method, int param, string target, int form)> evolutions)
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
            foreach (var (method, param, target, form) in evolutions)
            {
                var paramToken = param.ToString();
                if (method == "EVO_ITEM" || method == "EVO_TRADE_ITEM" || method == "EVO_STONE" || method == "EVO_STONE_MALE" || method == "EVO_STONE_FEMALE" || method == "EVO_ITEM_DAY" || method == "EVO_ITEM_NIGHT")
                {
                    if (HGParsers.TryGetItemMacro(param, out var mac)) paramToken = mac;
                }
                else if (method == "EVO_HAS_MOVE")
                {
                    if (HGParsers.TryGetMoveMacro(param, out var mac)) paramToken = mac;
                }
                else if (method == "EVO_HAS_MOVE_TYPE")
                {
                    if (HGParsers.TryGetTypeMacro(param, out var mac)) paramToken = mac;
                }
                else if (method == "EVO_OTHER_PARTY_MON" || method == "EVO_TRADE_SPECIFIC_MON")
                {
                    if (HGParsers.TryGetSpeciesMacro(param, out var mac)) paramToken = mac;
                }

                if (form > 0)
                    sb.AppendLine($"    evolution {method}, {paramToken}, {target} | ({form} << 11)");
                else
                    sb.AppendLine($"    evolution {method}, {paramToken}, {target}");
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


