using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HGEngineGUI.Pages
{
    public sealed partial class ConfigPage : Page
    {
        public ConfigPage()
        {
            InitializeComponent();
            _ = LoadAsync();
        }

        private string? ConfigPath => ProjectContext.RootPath == null ? null : Path.Combine(ProjectContext.RootPath, "include", "config.h");

        private async Task LoadAsync()
        {
            try
            {
                ConfigPathText.Text = ConfigPath ?? "(select a project)";
                if (ConfigPath == null || !File.Exists(ConfigPath))
                {
                    SetAll(false, isEnabled: false);
                    return;
                }
                var text = await File.ReadAllTextAsync(ConfigPath);
                SetFromConfig(text);
                SetAll(null, isEnabled: true);
            }
            catch { }
        }

        private void SetAll(bool? check = null, bool? isEnabled = null)
        {
            foreach (var cb in GetAllCheckBoxes())
            {
                if (check.HasValue) cb.IsChecked = check.Value;
                if (isEnabled.HasValue) cb.IsEnabled = isEnabled.Value;
            }
        }

        private IEnumerable<CheckBox> GetAllCheckBoxes()
        {
            return new[]
            {
                FairyType, HiddenAbilities, MegaEvolutions, PrimalReversion, ItemPocketExpansion, BdhcamRoutine,
                TransparentTextboxes, WildDoubleBattles, CaptureExperience, CriticalCapture, NewEvIvViewer,
                ImplementLevelCap, UncapCandies, AllowLevelCapEvolve, UpdateOverworldPoison, DisableEndOfTurnWeatherMsg,
                ExpandPcBoxes, FriendshipEffects, RestoreItemsAfterBattle, ReusableRepels, UpdateVitaminEvCaps,
                DisableItemsInTrainerBattle, ReusableTms, StaticHpBar, AllowSaveChanges, ImplementSeasons, ImplementDexitForms
            };
        }

        private static bool HasDefine(string text, string name)
        {
            // consider uncommented #define or value set to TRUE/1
            // Matches: #define NAME ... ; not commented out
            var rx = new Regex(@"^[ \t]*#define[ \t]+" + Regex.Escape(name) + @"\b", RegexOptions.Multiline);
            if (rx.IsMatch(text)) return true;
            // Also allow NAME TRUE/FALSE when defined as e.g., #define NAME TRUE
            var rxBool = new Regex(@"^[ \t]*#define[ \t]+" + Regex.Escape(name) + @"[ \t]+(TRUE|1)\b", RegexOptions.Multiline);
            if (rxBool.IsMatch(text)) return true;
            return false;
        }

        private static bool IsDefinedAsZero(string text, string name)
        {
            var rxZero = new Regex(@"^[ \t]*#define[ \t]+" + Regex.Escape(name) + @"[ \t]+0\b", RegexOptions.Multiline);
            return rxZero.IsMatch(text);
        }

        private void SetFromConfig(string text)
        {
            // FAIRY_TYPE_IMPLEMENTED uses numeric value (1/0)
            FairyType.IsChecked = HasDefine(text, "FAIRY_TYPE_IMPLEMENTED") && !IsDefinedAsZero(text, "FAIRY_TYPE_IMPLEMENTED");
            HiddenAbilities.IsChecked = HasDefine(text, "HIDDEN_ABILITIES");
            MegaEvolutions.IsChecked = HasDefine(text, "MEGA_EVOLUTIONS");
            PrimalReversion.IsChecked = HasDefine(text, "PRIMAL_REVERSION");
            ItemPocketExpansion.IsChecked = HasDefine(text, "ITEM_POCKET_EXPANSION");
            BdhcamRoutine.IsChecked = HasDefine(text, "IMPLEMENT_BDHCAM_ROUTINE");
            TransparentTextboxes.IsChecked = HasDefine(text, "IMPLEMENT_TRANSPARENT_TEXTBOXES");
            WildDoubleBattles.IsChecked = HasDefine(text, "IMPLEMENT_WILD_DOUBLE_BATTLES");
            CaptureExperience.IsChecked = HasDefine(text, "IMPLEMENT_CAPTURE_EXPERIENCE");
            CriticalCapture.IsChecked = HasDefine(text, "IMPLEMENT_CRITICAL_CAPTURE");
            NewEvIvViewer.IsChecked = HasDefine(text, "IMPLEMENT_NEW_EV_IV_VIEWER");
            ImplementLevelCap.IsChecked = HasDefine(text, "IMPLEMENT_LEVEL_CAP");
            UncapCandies.IsChecked = HasDefine(text, "UNCAP_CANDIES_FROM_LEVEL_CAP");
            AllowLevelCapEvolve.IsChecked = HasDefine(text, "ALLOW_LEVEL_CAP_EVOLVE");
            UpdateOverworldPoison.IsChecked = HasDefine(text, "UPDATE_OVERWORLD_POISON");
            DisableEndOfTurnWeatherMsg.IsChecked = HasDefine(text, "DISABLE_END_OF_TURN_WEATHER_MESSAGE");
            ExpandPcBoxes.IsChecked = HasDefine(text, "EXPAND_PC_BOXES");
            FriendshipEffects.IsChecked = HasDefine(text, "FRIENDSHIP_EFFECTS");
            RestoreItemsAfterBattle.IsChecked = HasDefine(text, "RESTORE_ITEMS_AT_BATTLE_END");
            ReusableRepels.IsChecked = HasDefine(text, "IMPLEMENT_REUSABLE_REPELS");
            UpdateVitaminEvCaps.IsChecked = HasDefine(text, "UPDATE_VITAMIN_EV_CAPS");
            DisableItemsInTrainerBattle.IsChecked = HasDefine(text, "DISABLE_ITEMS_IN_TRAINER_BATTLE");
            ReusableTms.IsChecked = HasDefine(text, "REUSABLE_TMS");
            StaticHpBar.IsChecked = HasDefine(text, "STATIC_HP_BAR");
            AllowSaveChanges.IsChecked = HasDefine(text, "ALLOW_SAVE_CHANGES");
            ImplementSeasons.IsChecked = HasDefine(text, "IMPLEMENT_SEASONS");
            ImplementDexitForms.IsChecked = HasDefine(text, "IMPLEMENT_DEXIT_FORMS_MECHANICS");
        }

        private async Task SaveAsync()
        {
            if (ConfigPath == null || !File.Exists(ConfigPath)) return;
            var original = await File.ReadAllTextAsync(ConfigPath);

            string ReplaceToggle(string text, string name, bool enabled)
            {
                // Strategy:
                // - If enabled: ensure an uncommented `#define NAME` exists (create if missing after the comment block for that section)
                // - If disabled: comment out any line defining NAME; if value form exists like `#define NAME TRUE`, set to FALSE and keep define uncommented if needed? In this codebase, most toggles are presence defines, so comment out.
                // 1) Try to modify an existing active #define NAME line
                var rxLine = new Regex(@"^(?<pre>[ \t]*)#define[ \t]+" + Regex.Escape(name) + @"(?<rest>.*)$", RegexOptions.Multiline);
                if (!enabled)
                {
                    text = rxLine.Replace(text, m => m.Groups["pre"].Value + "//#define " + name + m.Groups["rest"].Value);
                }

                // 2) If enabling, first try to uncomment a commented-out line like // #define NAME ...
                if (enabled)
                {
                    var rxCommented = new Regex(@"^(?<pre>[ \t]*)//[ \t]*#define[ \t]+" + Regex.Escape(name) + @"(?<rest>.*)$", RegexOptions.Multiline);
                    var before = text;
                    text = rxCommented.Replace(text, m => m.Groups["pre"].Value + "#define " + name + m.Groups["rest"].Value, 1);
                    // 3) If still no active define, insert one just before the closing #endif
                    var rxActive = new Regex(@"^[ \t]*#define[ \t]+" + Regex.Escape(name) + @"\b", RegexOptions.Multiline);
                    if (!rxActive.IsMatch(text))
                    {
                        int insertAt = text.LastIndexOf("\n#endif", StringComparison.Ordinal);
                        if (insertAt < 0) insertAt = text.Length;
                        text = text.Insert(insertAt, "\n#define " + name + "\n");
                    }
                }
                return text;
            }

            string ReplaceNumericDefine(string text, string name, string valueWhenEnabled, string valueWhenDisabled)
            {
                // Replace or add a numeric define as `#define NAME <value>`
                var rxNum = new Regex(@"^(?<pre>[ \t]*)#define[ \t]+" + Regex.Escape(name) + @"[ \t]+(?<val>\S+).*$", RegexOptions.Multiline);
                if (rxNum.IsMatch(text))
                {
                    text = rxNum.Replace(text, m => m.Groups["pre"].Value + "#define " + name + " " + valueWhenEnabled);
                }
                else
                {
                    // Try to uncomment a commented numeric define
                    var rxCommented = new Regex(@"^(?<pre>[ \t]*)//[ \t]*#define[ \t]+" + Regex.Escape(name) + @"[ \t]+(?<val>\S+).*$", RegexOptions.Multiline);
                    if (rxCommented.IsMatch(text))
                    {
                        text = rxCommented.Replace(text, m => m.Groups["pre"].Value + "#define " + name + " " + valueWhenEnabled);
                    }
                    else
                    {
                        int insertAt = text.LastIndexOf("\n#endif", StringComparison.Ordinal);
                        if (insertAt < 0) insertAt = text.Length;
                        text = text.Insert(insertAt, "\n#define " + name + " " + valueWhenEnabled + "\n");
                    }
                }
                return text;
            }

            string updated = original;
            // Core
            if (FairyType.IsChecked == true)
                updated = ReplaceNumericDefine(updated, "FAIRY_TYPE_IMPLEMENTED", "1", "0");
            else
                updated = ReplaceNumericDefine(updated, "FAIRY_TYPE_IMPLEMENTED", "0", "0");
            updated = ReplaceToggle(updated, "HIDDEN_ABILITIES", HiddenAbilities.IsChecked == true);
            updated = ReplaceToggle(updated, "MEGA_EVOLUTIONS", MegaEvolutions.IsChecked == true);
            updated = ReplaceToggle(updated, "PRIMAL_REVERSION", PrimalReversion.IsChecked == true);
            updated = ReplaceToggle(updated, "ITEM_POCKET_EXPANSION", ItemPocketExpansion.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_BDHCAM_ROUTINE", BdhcamRoutine.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_TRANSPARENT_TEXTBOXES", TransparentTextboxes.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_WILD_DOUBLE_BATTLES", WildDoubleBattles.IsChecked == true);
            // Battle/Progression
            updated = ReplaceToggle(updated, "IMPLEMENT_CAPTURE_EXPERIENCE", CaptureExperience.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_CRITICAL_CAPTURE", CriticalCapture.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_NEW_EV_IV_VIEWER", NewEvIvViewer.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_LEVEL_CAP", ImplementLevelCap.IsChecked == true);
            updated = ReplaceToggle(updated, "UNCAP_CANDIES_FROM_LEVEL_CAP", UncapCandies.IsChecked == true);
            updated = ReplaceToggle(updated, "ALLOW_LEVEL_CAP_EVOLVE", AllowLevelCapEvolve.IsChecked == true);
            updated = ReplaceToggle(updated, "UPDATE_OVERWORLD_POISON", UpdateOverworldPoison.IsChecked == true);
            updated = ReplaceToggle(updated, "DISABLE_END_OF_TURN_WEATHER_MESSAGE", DisableEndOfTurnWeatherMsg.IsChecked == true);
            updated = ReplaceToggle(updated, "EXPAND_PC_BOXES", ExpandPcBoxes.IsChecked == true);
            updated = ReplaceToggle(updated, "FRIENDSHIP_EFFECTS", FriendshipEffects.IsChecked == true);
            updated = ReplaceToggle(updated, "RESTORE_ITEMS_AT_BATTLE_END", RestoreItemsAfterBattle.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_REUSABLE_REPELS", ReusableRepels.IsChecked == true);
            updated = ReplaceToggle(updated, "UPDATE_VITAMIN_EV_CAPS", UpdateVitaminEvCaps.IsChecked == true);
            updated = ReplaceToggle(updated, "DISABLE_ITEMS_IN_TRAINER_BATTLE", DisableItemsInTrainerBattle.IsChecked == true);
            updated = ReplaceToggle(updated, "REUSABLE_TMS", ReusableTms.IsChecked == true);
            updated = ReplaceToggle(updated, "STATIC_HP_BAR", StaticHpBar.IsChecked == true);
            // Other
            updated = ReplaceToggle(updated, "ALLOW_SAVE_CHANGES", AllowSaveChanges.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_SEASONS", ImplementSeasons.IsChecked == true);
            updated = ReplaceToggle(updated, "IMPLEMENT_DEXIT_FORMS_MECHANICS", ImplementDexitForms.IsChecked == true);

            // Ensure both lines are explicitly handled for Level Cap
            if (ImplementLevelCap.IsChecked == true)
            {
                // 1) IMPLEMENT_LEVEL_CAP: uncomment or add
                var rxActiveImpl = new Regex(@"^[ \t]*#define[ \t]+IMPLEMENT_LEVEL_CAP\b", RegexOptions.Multiline);
                if (!rxActiveImpl.IsMatch(updated))
                {
                    var rxCommentedImpl = new Regex(@"^(?<pre>[ \t]*)//[ \t]*#define[ \t]+IMPLEMENT_LEVEL_CAP\b.*$", RegexOptions.Multiline);
                    if (rxCommentedImpl.IsMatch(updated))
                    {
                        updated = rxCommentedImpl.Replace(updated, m => m.Groups["pre"].Value + "#define IMPLEMENT_LEVEL_CAP", 1);
                    }
                    else
                    {
                        int insertAt = updated.LastIndexOf("\n#endif", StringComparison.Ordinal);
                        if (insertAt < 0) insertAt = updated.Length;
                        updated = updated.Insert(insertAt, "\n#define IMPLEMENT_LEVEL_CAP\n");
                    }
                }

                // 2) LEVEL_CAP_VARIABLE: uncomment or add with default value if missing
                var rxActiveVar = new Regex(@"^[ \t]*#define[ \t]+LEVEL_CAP_VARIABLE\b", RegexOptions.Multiline);
                if (!rxActiveVar.IsMatch(updated))
                {
                    var rxCommentedVar = new Regex(@"^(?<pre>[ \t]*)//[ \t]*#define[ \t]+LEVEL_CAP_VARIABLE[ \t]+(?<val>\S+).*$", RegexOptions.Multiline);
                    if (rxCommentedVar.IsMatch(updated))
                    {
                        updated = rxCommentedVar.Replace(updated, m => m.Groups["pre"].Value + "#define LEVEL_CAP_VARIABLE " + m.Groups["val"].Value, 1);
                    }
                    else
                    {
                        int insertAt2 = updated.LastIndexOf("\n#endif", StringComparison.Ordinal);
                        if (insertAt2 < 0) insertAt2 = updated.Length;
                        updated = updated.Insert(insertAt2, "\n#define LEVEL_CAP_VARIABLE 0x416F\n");
                    }
                }
            }
            else
            {
                // If Level Cap disabled, comment both out to match expected behavior
                var rxImpl = new Regex(@"^(?<pre>[ \t]*)#define[ \t]+IMPLEMENT_LEVEL_CAP\b(?<rest>.*)$", RegexOptions.Multiline);
                updated = rxImpl.Replace(updated, m => m.Groups["pre"].Value + "//#define IMPLEMENT_LEVEL_CAP" + m.Groups["rest"].Value);
                var rxVar = new Regex(@"^(?<pre>[ \t]*)#define[ \t]+LEVEL_CAP_VARIABLE[ \t]+(?<val>\S+).*$", RegexOptions.Multiline);
                updated = rxVar.Replace(updated, m => m.Groups["pre"].Value + "//#define LEVEL_CAP_VARIABLE " + m.Groups["val"].Value);
            }

            if (!string.Equals(updated, original, StringComparison.Ordinal))
            {
                var backup = ConfigPath + ".bak";
                try { File.Copy(ConfigPath, backup, true); } catch { }
                await File.WriteAllTextAsync(ConfigPath, updated);
                HGEngineGUI.Services.ChangeLog.Record(ConfigPath, updated.Length);
                (App.MainWindow as HGEngineGUI.MainWindow)?.UpdateStatus("Saved config");
            }
            else
            {
                (App.MainWindow as HGEngineGUI.MainWindow)?.UpdateStatus("No changes");
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            await SaveAsync();
        }

        private async void OnReloadClick(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
        }
    }
}


