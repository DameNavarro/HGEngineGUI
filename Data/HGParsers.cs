using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HGEngineGUI.Pages;

namespace HGEngineGUI.Data
{
    public static class HGParsers
    {
        private static readonly Regex DefineRegex = new(@"^\s*#define\s+(?<name>[A-Z0-9_]+)\s+(?<value>[-+*/()A-Z0-9_]+)\s*$", RegexOptions.Compiled);

        public static IReadOnlyList<SpeciesEntry> Species => _species;
        private static List<SpeciesEntry> _species = new();
        public static IReadOnlyList<string> SpeciesMacroNames => _speciesMacroNames;
        private static List<string> _speciesMacroNames = new();
        public static IReadOnlyList<(int Id, string Name)> TrainerClasses => _trainerClasses;
        private static List<(int Id, string Name)> _trainerClasses = new();
        public static IReadOnlyList<string> ItemMacros => _itemMacros;
        private static List<string> _itemMacros = new();
        public static IReadOnlyList<string> TypeMacros => _typeMacros;
        private static List<string> _typeMacros = new();
        public static IReadOnlyList<string> AbilityMacros => _abilityMacros;
        private static List<string> _abilityMacros = new();
        public static IReadOnlyList<string> EggGroupMacros => _eggGroupMacros;
        private static List<string> _eggGroupMacros = new();
        public static IReadOnlyList<string> GrowthRateMacros => _growthRateMacros;
        private static List<string> _growthRateMacros = new();
        public static IReadOnlyList<string> EvolutionMethodMacros => _evoMethodMacros;
        private static List<string> _evoMethodMacros = new();
        public static IReadOnlyList<string> MapMacros => _mapMacros;
        private static List<string> _mapMacros = new();
        private static Dictionary<string,int> _mapValues = new(StringComparer.Ordinal);
        public static bool TryGetMapValue(string macro, out int value) => _mapValues.TryGetValue(macro, out value);

        // Trainer AI flag macros (F_*) parsed from armips/include/constants.s
        public static IReadOnlyList<string> AIFlagMacros => _aiFlagMacros;
        private static List<string> _aiFlagMacros = new();

        // Nature macros from include/pokemon.h (NATURE_*)
        public static IReadOnlyList<string> NatureMacros => _natureMacros;
        private static List<string> _natureMacros = new();

        // Ball macros (try armips/include/items or constants.s items set). We reuse ItemMacros and filter for BALL_ if available.
        public static IReadOnlyList<string> BallMacros => _ballMacros;
        private static List<string> _ballMacros = new();

        // Detail caches populated on demand for a selected species
        public static IReadOnlyList<(int level, string move)> LevelUpMoves => _levelUpMoves;
        public static IReadOnlyList<(string method, int param, string target)> Evolutions => _evolutions;
        public static IReadOnlyList<string> EggMoves => _eggMoves;
        private static List<(int level, string move)> _levelUpMoves = new();
        private static List<(string method, int param, string target)> _evolutions = new();
        private static List<string> _eggMoves = new();

        // Overview data
        public class SpeciesOverview
        {
            public int BaseHp { get; set; }
            public int BaseAttack { get; set; }
            public int BaseDefense { get; set; }
            public int BaseSpeed { get; set; }
            public int BaseSpAttack { get; set; }
            public int BaseSpDefense { get; set; }
            public string Type1 { get; set; } = string.Empty;
            public string Type2 { get; set; } = string.Empty;
            public int CatchRate { get; set; }
            public int BaseExp { get; set; }
            public (int hp,int atk,int def,int spd,int spatk,int spdef) EvYields { get; set; }
            public string Item1 { get; set; } = "ITEM_NONE";
            public string Item2 { get; set; } = "ITEM_NONE";
            public int GenderRatio { get; set; }
            public int EggCycles { get; set; }
            public int BaseFriendship { get; set; }
            public string GrowthRate { get; set; } = string.Empty;
            public string EggGroup1 { get; set; } = string.Empty;
            public string EggGroup2 { get; set; } = string.Empty;
            public string Ability1 { get; set; } = string.Empty;
            public string Ability2 { get; set; } = string.Empty;
            public string AbilityHidden { get; set; } = string.Empty;
            public int RunChance { get; set; }
            public string DexClassification { get; set; } = string.Empty;
            public string DexEntry { get; set; } = string.Empty;
            public string DexHeight { get; set; } = string.Empty; // textual, e.g., 3’03”
            public string DexWeight { get; set; } = string.Empty; // textual, e.g., 28.7 lbs.
        }

        // Trainers parsing (read-only summary)
        public record TrainerHeader(int Id, string Name, string Class, int NumMons, string AIFlags, string BattleType, IReadOnlyList<string> Items);
        public static IReadOnlyList<TrainerHeader> Trainers => _trainers;
        private static List<TrainerHeader> _trainers = new();

        public static async Task RefreshTrainersAsync()
        {
            _trainers = new();
            if (ProjectContext.RootPath == null) return;
            var trainersPath = PathTrainers ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            if (!File.Exists(trainersPath)) return;

            // Parse trainer headers: trainerdata <id>, "<name>" then fields until endentry
            var text = await File.ReadAllTextAsync(trainersPath);
            var trainerRegex = new Regex("trainerdata\\s+(?<id>\\d+),\\s*\"(?<name>[^\"]*)\"(?<body>[\\s\\S]*?)endentry", RegexOptions.Multiline);
            foreach (Match t in trainerRegex.Matches(text))
            {
                int id = int.Parse(t.Groups["id"].Value);
                string name = t.Groups["name"].Value;
                string body = t.Groups["body"].Value;

                string cls = MatchOne(body, @"trainerclass\s+(?<v>[A-Z0-9_]+)") ?? "";
                int numMons = int.TryParse(MatchOne(body, @"nummons\s+(?<v>\d+)"), out var nm) ? nm : 0;
                string ai = MatchOne(body, @"aiflags\s+(?<v>.*)")?.Trim() ?? "";
                string bt = MatchOne(body, @"battletype\s+(?<v>[A-Z0-9_]+)") ?? "";

                var items = new List<string>();
                var itemRegex = new Regex(@"^\s*item\s+(?<v>[A-Z0-9_]+)\s*$", RegexOptions.Multiline);
                foreach (Match im in itemRegex.Matches(body))
                {
                    items.Add(im.Groups["v"].Value);
                }
                _trainers.Add(new TrainerHeader(id, name, cls, numMons, ai, bt, items));
            }
        }

        private static string? MatchOne(string body, string pattern)
        {
            var m = Regex.Match(body, pattern);
            return m.Success ? m.Groups["v"].Value : null;
        }

        // Party details for a trainer
        public record TrainerMon(
            int Index,
            int Level,
            string Species,
            int AbilitySlot,
            int IVs,
            string Item,
            string Move1,
            string Move2,
            string Move3,
            string Move4,
            string Nature,
            int Form,
            string Ball,
            bool ShinyLock,
            string Nickname,
            string PP,
            string Ability,
            int BallSeal,
            string IVNums,
            string EVNums,
            int Status,
            int Hp,
            int Atk,
            int Def,
            int Speed,
            int SpAtk,
            int SpDef,
            string Type1,
            string Type2,
            string PPCounts,
            int AdditionalFlags
        );
        public static IReadOnlyList<TrainerMon> CurrentTrainerParty => _currentParty;
        private static List<TrainerMon> _currentParty = new();

        public static async Task RefreshTrainerDetailsAsync(int trainerId)
        {
            _currentParty = new List<TrainerMon>();
            if (ProjectContext.RootPath == null) return;
            var trainersPath = PathTrainers ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s");
            if (!File.Exists(trainersPath)) return;

            var text = await File.ReadAllTextAsync(trainersPath);
            // Find party block: party <id> ... endparty
            var partyRegex = new Regex($"party\\s+{trainerId}\\s*(?<body>[\\s\\S]*?)endparty", RegexOptions.Multiline);
            var match = partyRegex.Match(text);
            if (!match.Success) return;
            var body = match.Groups["body"].Value;

            // Parse sequential mon entries; rely on 'pokemon SPECIES_*' as delimiter
            var lines = body.Split('\n');
            int currentIndex = -1;
            int level = 0; int abilityslot = 0; int ivs = 0; string species = string.Empty; string item = "ITEM_NONE";
            string nature = string.Empty; int form = 0; string ball = string.Empty; bool shiny = false; string nickname = string.Empty; string pp = string.Empty;
            string ability = string.Empty; int ballseal = 0; string ivnums = string.Empty; string evnums = string.Empty; string ppcounts = string.Empty;
            int status = 0; int hp = 0; int atk = 0; int def = 0; int speed = 0; int spatk = 0; int spdef = 0; int additionalflags = 0;
            string type1 = string.Empty; string type2 = string.Empty;
            var moves = new List<string>(4);
            void FlushIfComplete()
            {
                if (!string.IsNullOrEmpty(species))
                {
                    while (moves.Count < 4) moves.Add("MOVE_NONE");
                    _currentParty.Add(new TrainerMon(
                        currentIndex < 0 ? _currentParty.Count : currentIndex,
                        level,
                        species,
                        abilityslot,
                        ivs,
                        item,
                        moves[0], moves[1], moves[2], moves[3],
                        nature,
                        form,
                        ball,
                        shiny,
                        nickname,
                        pp,
                        ability,
                        ballseal,
                        ivnums,
                        evnums,
                        status,
                        hp,
                        atk,
                        def,
                        speed,
                        spatk,
                        spdef,
                        type1,
                        type2,
                        ppcounts,
                        additionalflags
                    ));
                    level = 0; abilityslot = 0; ivs = 0; species = string.Empty; currentIndex = -1; item = "ITEM_NONE"; moves.Clear(); nature = string.Empty; form = 0; ball = string.Empty; shiny = false; nickname = string.Empty; pp = string.Empty;
                    ability = string.Empty; ballseal = 0; ivnums = string.Empty; evnums = string.Empty; ppcounts = string.Empty;
                    status = 0; hp = atk = def = speed = spatk = spdef = 0; additionalflags = 0; type1 = type2 = string.Empty;
                }
            }
            var idxRegex = new Regex(@"mon\s+(?<i>\d+)");
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("// mon "))
                {
                    // Starting a new mon: flush the previous one first
                    FlushIfComplete();
                    var m = idxRegex.Match(line.Replace("// ", string.Empty));
                    if (m.Success) currentIndex = int.Parse(m.Groups["i"].Value);
                    continue;
                }
                if (line.StartsWith("ivs "))
                {
                    int.TryParse(line.Substring(4).Trim(), out ivs);
                    continue;
                }
                if (line.StartsWith("ivnums ")) { ivnums = line.Substring("ivnums ".Length).Trim(); continue; }
                if (line.StartsWith("abilityslot "))
                {
                    int.TryParse(line.Substring("abilityslot ".Length).Trim(), out abilityslot);
                    continue;
                }
                if (line.StartsWith("ability ")) { ability = line.Substring("ability ".Length).Trim(); continue; }
                if (line.StartsWith("level "))
                {
                    int.TryParse(line.Substring(6).Trim(), out level);
                    continue;
                }
                if (line.StartsWith("item "))
                {
                    item = line.Substring(5).Trim();
                    continue;
                }
                if (line.StartsWith("move "))
                {
                    var mv = line.Substring(5).Trim();
                    if (moves.Count < 4) moves.Add(mv);
                    continue;
                }
                if (line.StartsWith("nature "))
                {
                    nature = line.Substring("nature ".Length).Trim();
                    continue;
                }
                if (line.StartsWith("form "))
                {
                    int.TryParse(line.Substring("form ".Length).Trim(), out form);
                    continue;
                }
                if (line.StartsWith("ball "))
                {
                    ball = line.Substring("ball ".Length).Trim();
                    continue;
                }
                if (line.StartsWith("ballseal ")) { int.TryParse(line.Substring("ballseal ".Length).Trim(), out ballseal); continue; }
                if (line.StartsWith("shinylock") || line.StartsWith("shiny_lock") || line.StartsWith("shiny "))
                {
                    // Accept variants: "shinylock", "shinylock 1", "shiny 1"
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    shiny = parts.Length == 1 ? true : (parts.Length > 1 && (parts[1] == "1" || parts[1].Equals("true", StringComparison.OrdinalIgnoreCase)));
                    continue;
                }
                if (line.StartsWith("pp "))
                {
                    pp = line.Substring(3).Trim();
                    continue;
                }
                if (line.StartsWith("ppcounts ")) { ppcounts = line.Substring("ppcounts ".Length).Trim(); continue; }
                if (line.StartsWith("nickname "))
                {
                    nickname = line.Substring("nickname ".Length).Trim().Trim('"');
                    continue;
                }
                if (line.StartsWith("pokemon "))
                {
                    species = line.Substring(8).Trim();
                    continue;
                }
                if (line.StartsWith("evnums ")) { evnums = line.Substring("evnums ".Length).Trim(); continue; }
                if (line.StartsWith("status ")) { int.TryParse(line.Substring("status ".Length).Trim(), out status); continue; }
                if (line.StartsWith("hp ")) { int.TryParse(line.Substring(3).Trim(), out hp); continue; }
                if (line.StartsWith("atk ")) { int.TryParse(line.Substring(4).Trim(), out atk); continue; }
                if (line.StartsWith("def ")) { int.TryParse(line.Substring(4).Trim(), out def); continue; }
                if (line.StartsWith("speed ")) { int.TryParse(line.Substring(6).Trim(), out speed); continue; }
                if (line.StartsWith("spatk ")) { int.TryParse(line.Substring(6).Trim(), out spatk); continue; }
                if (line.StartsWith("spdef ")) { int.TryParse(line.Substring(6).Trim(), out spdef); continue; }
                if (line.StartsWith("types ")) { var parts2 = line.Substring(6).Split(','); if (parts2.Length >= 2) { type1 = parts2[0].Trim(); type2 = parts2[1].Trim(); } continue; }
                if (line.StartsWith("additionalflags ")) { int.TryParse(line.Substring("additionalflags ".Length).Trim(), out additionalflags); continue; }
            }
            // Flush the last mon at end of block
            FlushIfComplete();
        }
        public static SpeciesOverview? Overview { get; private set; }
        public static IReadOnlyList<string> TmHmMoves => _tmhmMoves;
        public static IReadOnlyList<(string tutor, string move, int cost)> TutorMoves => _tutorMoves;
        private static List<string> _tmhmMoves = new();
        private static List<(string tutor, string move, int cost)> _tutorMoves = new();
        public static IReadOnlyList<string> MoveMacros => _moveMacros;
        private static List<string> _moveMacros = new();
        private static Dictionary<string,int> _itemValues = new(StringComparer.Ordinal);
        private static Dictionary<string,int> _moveValues = new(StringComparer.Ordinal);
        public static bool TryGetItemValue(string macro, out int value) => _itemValues.TryGetValue(macro, out value);
        public static bool TryGetMoveValue(string macro, out int value) => _moveValues.TryGetValue(macro, out value);
        // Items and prices
        public static IReadOnlyList<(string ItemMacro, int Price)> ItemsWithPrices => _itemsWithPrices;
        private static List<(string ItemMacro, int Price)> _itemsWithPrices = new();
        public static string? PathItemData { get; private set; }
        public static string? PathMartItems { get; private set; }
        public static bool HasMartItems => PathMartItems != null && System.IO.File.Exists(PathMartItems);
        public static IReadOnlyCollection<string> TmHmSelectedForSpecies => _tmhmSelectedForSpecies;
        private static HashSet<string> _tmhmSelectedForSpecies = new();
        public static IReadOnlyList<(string tutor, string move, int cost)> TutorHeaders => _tutorHeaders;
        private static List<(string tutor, string move, int cost)> _tutorHeaders = new();
        public static IReadOnlyCollection<string> TutorSelectedForSpecies => _tutorSelectedForSpecies;
        private static HashSet<string> _tutorSelectedForSpecies = new();

        // Encounters
        public class EncounterSlot
        {
            public string SpeciesMacro { get; set; } = string.Empty;
            public int MinLevel { get; set; }
            public int MaxLevel { get; set; }
            public EncounterSlot() {}
            public EncounterSlot(string species, int min, int max) { SpeciesMacro = species; MinLevel = min; MaxLevel = max; }
        }
        public class EncounterArea
        {
            public int Id { get; set; }
            public string Label { get; set; } = string.Empty;
            public int WalkRate { get; set; }
            public int SurfRate { get; set; }
            public int RockSmashRate { get; set; }
            public int OldRodRate { get; set; }
            public int GoodRodRate { get; set; }
            public int SuperRodRate { get; set; }
            public int[] WalkLevels { get; set; } = new int[12];
            // Editable probability distributions (percent) shown in UI and written as comments.
            public int[] GrassProbabilities { get; set; } = new int[] { 20, 20, 10, 10, 10, 10, 5, 5, 4, 4, 1, 1 };
            public int[] SurfProbabilities { get; set; } = new int[] { 60, 30, 5, 4, 1 };
            public int[] RockProbabilities { get; set; } = new int[] { 90, 10 };
            public int[] OldRodProbabilities { get; set; } = new int[] { 60, 30, 5, 4, 1 };
            public int[] GoodRodProbabilities { get; set; } = new int[] { 40, 40, 15, 4, 1 };
            public int[] SuperRodProbabilities { get; set; } = new int[] { 40, 40, 15, 4, 1 };
            public List<string> MorningGrass { get; set; } = new();
            public List<string> DayGrass { get; set; } = new();
            public List<string> NightGrass { get; set; } = new();
            public List<string> HoennGrass { get; set; } = new();
            public List<string> SinnohGrass { get; set; } = new();
            public List<EncounterSlot> Surf { get; set; } = new();
            public List<EncounterSlot> RockSmash { get; set; } = new();
            public List<EncounterSlot> OldRod { get; set; } = new();
            public List<EncounterSlot> GoodRod { get; set; } = new();
            public List<EncounterSlot> SuperRod { get; set; } = new();
            public string SwarmGrass { get; set; } = string.Empty;
            public string SwarmSurf { get; set; } = string.Empty;
            public string SwarmGoodRod { get; set; } = string.Empty;
            public string SwarmSuperRod { get; set; } = string.Empty;
        }
        public static IReadOnlyList<EncounterArea> EncounterAreas => _encounterAreas;
        private static List<EncounterArea> _encounterAreas = new();

        public static string? PathEncounters { get; private set; }
        public static string? PathHeadbutt { get; private set; }
        // Mart editor
        public class MartItemEntry
        {
            public string Item { get; set; } = string.Empty; // ITEM_*
            public string? BadgeMacro { get; set; } // ZERO_BADGES..EIGHT_BADGES for general table
        }
        public class MartSection
        {
            public string Key { get; set; } = string.Empty; // .org address string like 0x020FBA54
            public string Label { get; set; } = string.Empty; // friendly label from comments or address fallback
            public bool IsGeneralTable { get; set; }
            public List<MartItemEntry> Items { get; set; } = new();
        }
        public static IReadOnlyList<MartSection> MartSections => _martSections;
        private static List<MartSection> _martSections = new();

        public static async Task RefreshEncountersAsync()
        {
            _encounterAreas = new();
            if (ProjectContext.RootPath == null) return;
            var path = PathEncounters ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "encounters.s");
            if (!File.Exists(path)) return;

            var text = await File.ReadAllTextAsync(path);
            text = text.Replace("\r\n", "\n");
            // Capture optional inline label comment safely up to end of line
            var blocks = Regex.Matches(text, @"encounterdata\s+(?<id>\d+)\s*(?:\/\/\s*(?<label>[^\n\r]*))?(?<body>[\s\S]*?)\.close", RegexOptions.Multiline);
            foreach (Match b in blocks)
            {
                var area = new EncounterArea
                {
                    Id = int.Parse(b.Groups["id"].Value),
                    Label = b.Groups["label"].Value.Trim(),
                };

                var body = b.Groups["body"].Value;
                area.WalkRate = ReadInt(body, @"walkrate\s+(?<v>\d+)");
                area.SurfRate = ReadInt(body, @"surfrate\s+(?<v>\d+)");
                area.RockSmashRate = ReadInt(body, @"rocksmashrate\s+(?<v>\d+)");
                area.OldRodRate = ReadInt(body, @"oldrodrate\s+(?<v>\d+)");
                area.GoodRodRate = ReadInt(body, @"goodrodrate\s+(?<v>\d+)");
                area.SuperRodRate = ReadInt(body, @"superrodrate\s+(?<v>\d+)");

                var wl = Regex.Match(body, @"walklevels\s+(?<vals>[\d,\s]+)");
                if (wl.Success)
                {
                    var nums = wl.Groups["vals"].Value.Split(',').Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).ToArray();
                    for (int i = 0; i < Math.Min(12, nums.Length); i++) area.WalkLevels[i] = nums[i];
                }

                // Helper to parse 12 pokemon lines for each grass time block
                List<string> Parse12(string tag)
                {
                    var list = new List<string>(12);
                    var rx = new Regex(@"//\s*" + tag + @"[\s\S]*?(?<lines>(?:\s*pokemon\s+SPECIES_[A-Z0-9_]+\s*\n){1,12})", RegexOptions.Multiline);
                    var m = rx.Match(body);
                    if (m.Success)
                    {
                        var lr = new Regex(@"pokemon\s+(?<sp>SPECIES_[A-Z0-9_]+)");
                        foreach (Match mm in lr.Matches(m.Groups["lines"].Value)) list.Add(mm.Groups["sp"].Value);
                    }
                    return list;
                }

                area.MorningGrass = Parse12("morning encounter slots");
                area.DayGrass = Parse12("day encounter slots");
                area.NightGrass = Parse12("night encounter slots");
                area.HoennGrass = Parse12("hoenn encounter slots");
                area.SinnohGrass = Parse12("sinnoh encounter slots");

                // Encounter slots helper
                List<EncounterSlot> ParseEncounters(string header)
                {
                    var list = new List<EncounterSlot>();
                    var rx = new Regex(@"//\s*" + header + @"[\s\S]*?(?<lines>(?:\s*encounter\s+SPECIES_[A-Z0-9_]+\s*,\s*\d+\s*,\s*\d+\s*\n){1,100})", RegexOptions.Multiline);
                    var m = rx.Match(body);
                    if (m.Success)
                    {
                        var lr = new Regex(@"encounter\s+(?<sp>SPECIES_[A-Z0-9_]+)\s*,\s*(?<min>\d+)\s*,\s*(?<max>\d+)");
                        foreach (Match mm in lr.Matches(m.Groups["lines"].Value))
                        {
                            list.Add(new EncounterSlot(mm.Groups["sp"].Value, int.Parse(mm.Groups["min"].Value), int.Parse(mm.Groups["max"].Value)));
                        }
                    }
                    return list;
                }

                area.Surf = ParseEncounters("surf encounters");
                area.RockSmash = ParseEncounters("rock smash encounters");
                area.OldRod = ParseEncounters("old rod encounters");
                area.GoodRod = ParseEncounters("good rod encounters");
                area.SuperRod = ParseEncounters("super rod encounters");

                area.SwarmGrass = ReadSpecies(body, @"//\s*swarm grass\s*\n\s*pokemon\s+(?<sp>SPECIES_[A-Z0-9_]+)");
                area.SwarmSurf = ReadSpecies(body, @"//\s*swarm surf\s*\n\s*pokemon\s+(?<sp>SPECIES_[A-Z0-9_]+)");
                area.SwarmGoodRod = ReadSpecies(body, @"//\s*swarm good rod\s*\n\s*pokemon\s+(?<sp>SPECIES_[A-Z0-9_]+)");
                area.SwarmSuperRod = ReadSpecies(body, @"//\s*swarm super rod\s*\n\s*pokemon\s+(?<sp>SPECIES_[A-Z0-9_]+)");

                _encounterAreas.Add(area);
            }
        }

        private static int ReadInt(string body, string pattern)
        {
            var m = Regex.Match(body, pattern);
            return m.Success ? int.Parse(m.Groups["v"].Value) : 0;
        }

        private static string ReadSpecies(string body, string pattern)
        {
            var m = Regex.Match(body, pattern);
            return m.Success ? m.Groups["sp"].Value : string.Empty;
        }

        // Headbutt parsing
        public class HeadbuttArea
        {
            public int Id { get; set; }
            public int NormalCount { get; set; }
            public int SpecialCount { get; set; }
            public List<EncounterSlot> Normal { get; set; } = new();
            public List<EncounterSlot> Special { get; set; } = new();
        }
        public static IReadOnlyDictionary<int, HeadbuttArea> HeadbuttAreas => _headbuttAreas;
        private static Dictionary<int, HeadbuttArea> _headbuttAreas = new();

        public static async Task RefreshHeadbuttAsync()
        {
            _headbuttAreas = new();
            if (ProjectContext.RootPath == null) return;
            var path = PathHeadbutt ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "headbutt.s");
            if (!File.Exists(path)) return;
            var text = await File.ReadAllTextAsync(path);
            text = text.Replace("\r\n", "\n");
            var blocks = Regex.Matches(text, @"headbuttheader\s+(?<id>\d+)\s*,\s*(?<n>\d+)\s*,\s*(?<s>\d+)(?<body>[\s\S]*?)\.close", RegexOptions.Multiline);
            foreach (Match b in blocks)
            {
                int id = int.Parse(b.Groups["id"].Value);
                int n = int.Parse(b.Groups["n"].Value);
                int s = int.Parse(b.Groups["s"].Value);
                var area = new HeadbuttArea { Id = id, NormalCount = n, SpecialCount = s };
                var body = b.Groups["body"].Value;
                // Normal slots appear after '// normal slots'
                var normalMatch = Regex.Match(body, @"//\s*normal slots(?<lines>[\s\S]*?)//\s*special slots", RegexOptions.Multiline);
                if (normalMatch.Success)
                {
                    var lr = new Regex(@"headbuttencounter\s+(?<sp>SPECIES_[A-Z0-9_]+)\s*,\s*(?<min>\d+)\s*,\s*(?<max>\d+)");
                    foreach (Match mm in lr.Matches(normalMatch.Groups["lines"].Value))
                    {
                        area.Normal.Add(new EncounterSlot(mm.Groups["sp"].Value, int.Parse(mm.Groups["min"].Value), int.Parse(mm.Groups["max"].Value)));
                    }
                }
                var specialMatch = Regex.Match(body, @"//\s*special slots(?<lines>[\s\S]*?)//\s*normal trees|$", RegexOptions.Multiline);
                if (specialMatch.Success)
                {
                    var lr = new Regex(@"headbuttencounter\s+(?<sp>SPECIES_[A-Z0-9_]+)\s*,\s*(?<min>\d+)\s*,\s*(?<max>\d+)");
                    foreach (Match mm in lr.Matches(specialMatch.Groups["lines"].Value))
                    {
                        area.Special.Add(new EncounterSlot(mm.Groups["sp"].Value, int.Parse(mm.Groups["min"].Value), int.Parse(mm.Groups["max"].Value)));
                    }
                }
                _headbuttAreas[id] = area;
            }
        }

        public static async Task RefreshCachesAsync()
        {
            _species = new();
            if (ProjectContext.RootPath == null)
                return;

            // Resolve data file paths once per session
            ResolveDataPaths();

            // species.h
            var speciesHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "species.h");
            if (File.Exists(speciesHeader))
            {
                var lines = await File.ReadAllLinesAsync(speciesHeader);
                foreach (var line in lines)
                {
                    var m = DefineRegex.Match(line);
                    if (!m.Success) continue;
                    var name = m.Groups["name"].Value;
                    if (!name.StartsWith("SPECIES_", StringComparison.Ordinal)) continue;
                    if (!int.TryParse(m.Groups["value"].Value, out var id)) continue;
                    _species.Add(new SpeciesEntry(id, name));
                }
                _species.Sort((a, b) => a.Id.CompareTo(b.Id));
                ProjectContext.SpeciesCount = _species.Count;
                _speciesMacroNames = _species.Select(s => s.Name).ToList();
            }

            // trainerclass.h
            var trainerClassHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "trainerclass.h");
            _trainerClasses = new();
            if (File.Exists(trainerClassHeader))
            {
                var lines = await File.ReadAllLinesAsync(trainerClassHeader);
                foreach (var line in lines)
                {
                    var m = DefineRegex.Match(line);
                    if (!m.Success) continue;
                    var name = m.Groups["name"].Value;
                    if (!name.StartsWith("TRAINERCLASS_", StringComparison.Ordinal)) continue;
                    if (!int.TryParse(m.Groups["value"].Value, out var id)) continue;
                    _trainerClasses.Add((id, name));
                }
                _trainerClasses.Sort((a, b) => a.Id.CompareTo(b.Id));
            }

            // item.h macros (for item dropdowns)
            var itemHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "item.h");
            _itemMacros = new();
            if (File.Exists(itemHeader))
            {
                _itemValues = new(StringComparer.Ordinal);
                foreach (var line in await File.ReadAllLinesAsync(itemHeader))
                {
                    var m = DefineRegex.Match(line);
                    if (!m.Success) continue;
                    var name = m.Groups["name"].Value;
                    if (name.StartsWith("ITEM_", StringComparison.Ordinal))
                    {
                        _itemMacros.Add(name);
                        if (int.TryParse(m.Groups["value"].Value, out var iv)) _itemValues[name] = iv;
                    }
                }
            }

            // types come from include/battle.h in this codebase
            _typeMacros = new();
            var typeHeader = Path.Combine(ProjectContext.RootPath, "include", "battle.h");
            if (File.Exists(typeHeader))
            {
                foreach (var line in await File.ReadAllLinesAsync(typeHeader))
                {
                    var m = Regex.Match(line, @"^\s*#define\s+(TYPE_[A-Z0-9_]+)\s+\d+");
                    if (!m.Success) continue;
                    var name = m.Groups[1].Value;
                    if (!_typeMacros.Contains(name)) _typeMacros.Add(name);
                }
            }

            // Abilities: prefer asm/include/abilities.inc if present; else fallback to include/constants/ability.h
            _abilityMacros = new();
            var abilitiesInc = Path.Combine(ProjectContext.RootPath, "asm", "include", "abilities.inc");
            if (File.Exists(abilitiesInc))
            {
                foreach (var line in await File.ReadAllLinesAsync(abilitiesInc))
                {
                    var m = Regex.Match(line, @"^\s*\.equ\s+(ABILITY_[A-Z0-9_]+)\s*,\s*\d+");
                    if (!m.Success) continue;
                    var name = m.Groups[1].Value;
                    if (!_abilityMacros.Contains(name)) _abilityMacros.Add(name);
                }
            }
            else
            {
                var abilityHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "ability.h");
                if (File.Exists(abilityHeader))
                {
                    foreach (var line in await File.ReadAllLinesAsync(abilityHeader))
                    {
                        var m = DefineRegex.Match(line);
                        if (!m.Success) continue;
                        var name = m.Groups["name"].Value;
                        if (name.StartsWith("ABILITY_", StringComparison.Ordinal)) _abilityMacros.Add(name);
                    }
                }
            }

            // egg groups: may not exist; fallback to common set if header is missing
            _eggGroupMacros = new();
            var eggHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "egg_groups.h");
            if (File.Exists(eggHeader))
            {
                foreach (var line in await File.ReadAllLinesAsync(eggHeader))
                {
                    var m = DefineRegex.Match(line);
                    if (!m.Success) continue;
                    var name = m.Groups["name"].Value;
                    if (name.StartsWith("EGG_GROUP_", StringComparison.Ordinal)) _eggGroupMacros.Add(name);
                }
            }
            else
            {
                _eggGroupMacros.AddRange(new[]{
                    "EGG_GROUP_NONE","EGG_GROUP_MONSTER","EGG_GROUP_WATER_1","EGG_GROUP_BUG","EGG_GROUP_FLYING",
                    "EGG_GROUP_FIELD","EGG_GROUP_FAIRY","EGG_GROUP_GRASS","EGG_GROUP_HUMAN_LIKE","EGG_GROUP_WATER_3",
                    "EGG_GROUP_MINERAL","EGG_GROUP_AMORPHOUS","EGG_GROUP_WATER_2","EGG_GROUP_DITTO","EGG_GROUP_DRAGON",
                    "EGG_GROUP_UNDISCOVERED"
                });
            }

            // Growth rate macros: in this codebase under armips/include/constants.s
            _growthRateMacros = new();
            var growthAsm = Path.Combine(ProjectContext.RootPath, "armips", "include", "constants.s");
            if (File.Exists(growthAsm))
            {
                foreach (var line in await File.ReadAllLinesAsync(growthAsm))
                {
                    var m = Regex.Match(line, @"^\s*\.equ\s+(GROWTH_[A-Z0-9_]+)\s*,");
                    if (!m.Success) continue;
                    var name = m.Groups[1].Value;
                    if (!_growthRateMacros.Contains(name)) _growthRateMacros.Add(name);
                }
            }
            if (_growthRateMacros.Count == 0)
            {
                // Fallback to constants header if present
                var growthHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "growth.h");
                if (!File.Exists(growthHeader)) growthHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "growthrates.h");
                if (File.Exists(growthHeader))
                {
                    foreach (var line in await File.ReadAllLinesAsync(growthHeader))
                    {
                        var m = DefineRegex.Match(line);
                        if (!m.Success) continue;
                        var name = m.Groups["name"].Value;
                        if (name.StartsWith("GROWTH_", StringComparison.Ordinal)) _growthRateMacros.Add(name);
                    }
                }
            }

            // Evolution method macros (EVO_*)
            _evoMethodMacros = new();
            var evoHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "evolution.h");
            if (File.Exists(evoHeader))
            {
                foreach (var line in await File.ReadAllLinesAsync(evoHeader))
                {
                    var m = DefineRegex.Match(line);
                    if (!m.Success) continue;
                    var name = m.Groups["name"].Value;
                    if (name.StartsWith("EVO_", StringComparison.Ordinal)) _evoMethodMacros.Add(name);
                }
            }

            // Map macros for EVO_MAP (best-effort)
            _mapMacros = new();
            _mapValues = new(StringComparer.Ordinal);
            var mapHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "map.h");
            if (!File.Exists(mapHeader)) mapHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "map_groups.h");
            if (File.Exists(mapHeader))
            {
                foreach (var line in await File.ReadAllLinesAsync(mapHeader))
                {
                    var m = DefineRegex.Match(line);
                    if (!m.Success) continue;
                    var name = m.Groups["name"].Value;
                    if (name.StartsWith("MAP_", StringComparison.Ordinal))
                    {
                        _mapMacros.Add(name);
                        if (int.TryParse(m.Groups["value"].Value, out var mv)) _mapValues[name] = mv;
                    }
                }
            }
            // Ensure EVO_* macros are populated even if evolution.h is absent
            if (_evoMethodMacros.Count == 0)
            {
                var constantsDir = Path.Combine(ProjectContext.RootPath, "include", "constants");
                if (Directory.Exists(constantsDir))
                {
                    foreach (var file in Directory.EnumerateFiles(constantsDir, "*.h", SearchOption.AllDirectories))
                    {
                        foreach (var line in await File.ReadAllLinesAsync(file))
                        {
                            var m = DefineRegex.Match(line);
                            if (!m.Success) continue;
                            var name = m.Groups["name"].Value;
                            if (name.StartsWith("EVO_", StringComparison.Ordinal) && !_evoMethodMacros.Contains(name))
                                _evoMethodMacros.Add(name);
                        }
                    }
                }
            }

            // Guarantee common entries exist
            if (!_evoMethodMacros.Contains("EVO_NONE")) _evoMethodMacros.Insert(0, "EVO_NONE");
            if (!_evoMethodMacros.Contains("EVO_LEVEL")) _evoMethodMacros.Insert(0, "EVO_LEVEL");

            // moves.h (for move dropdowns)
            var movesHeader = Path.Combine(ProjectContext.RootPath, "include", "constants", "moves.h");
            _moveMacros = new();
            if (File.Exists(movesHeader))
            {
                _moveValues = new(StringComparer.Ordinal);
                foreach (var line in await File.ReadAllLinesAsync(movesHeader))
                {
                    var m = DefineRegex.Match(line);
                    if (!m.Success) continue;
                    var name = m.Groups["name"].Value;
                    if (name.StartsWith("MOVE_", StringComparison.Ordinal))
                    {
                        _moveMacros.Add(name);
                        if (int.TryParse(m.Groups["value"].Value, out var mv)) _moveValues[name] = mv;
                    }
                }
            }

            // Trainer AI flags (F_*) from armips/include/constants.s
            _aiFlagMacros = new();
            try
            {
                var constantsAsm = Path.Combine(ProjectContext.RootPath, "armips", "include", "constants.s");
                if (File.Exists(constantsAsm))
                {
                    foreach (var line in await File.ReadAllLinesAsync(constantsAsm))
                    {
                        var m = Regex.Match(line, @"^\s*\.equ\s+(F_[A-Z0-9_]+)\s*,");
                        if (!m.Success) continue;
                        var name = m.Groups[1].Value;
                        if (!_aiFlagMacros.Contains(name)) _aiFlagMacros.Add(name);
                    }
                }
            }
            catch { }

            // Nature macros
            try
            {
                var pokemonHeader = Path.Combine(ProjectContext.RootPath, "include", "pokemon.h");
                if (File.Exists(pokemonHeader))
                {
                    foreach (var line in await File.ReadAllLinesAsync(pokemonHeader))
                    {
                        var m = Regex.Match(line, @"#define\s+(NATURE_[A-Z_]+)\s*\(\d+\)");
                        if (!m.Success) continue;
                        var name = m.Groups[1].Value;
                        if (!_natureMacros.Contains(name)) _natureMacros.Add(name);
                    }
                }
            }
            catch { }

            // Ball macros: heuristic gather from ItemMacros if defined as BALL_
            _ballMacros = ItemMacros.Where(i => i.StartsWith("ITEM_", StringComparison.Ordinal) && (i.Contains("BALL") || i.EndsWith("_BALL")).Equals(true)).ToList();

            // tmlearnset.txt headers list for display
            _tmhmMoves = new();
            var tmPath = PathTm ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "tmlearnset.txt");
            if (File.Exists(tmPath))
            {
                var content = await File.ReadAllTextAsync(tmPath);
                content = content.Replace("\r\n", "\n");
                var headerRegex = new Regex(@"^(TM|HM)\d{3}:\s+[A-Z0-9_]+\s*$", RegexOptions.Multiline);
                foreach (Match m in headerRegex.Matches(content))
                {
                    var header = m.Value.Trim();
                    if (!_tmhmMoves.Contains(header)) _tmhmMoves.Add(header);
                }
            }

            // Items with prices from data/itemdata/itemdata.c (.price fields by index)
            _itemsWithPrices = new();
            if (PathItemData != null && File.Exists(PathItemData))
            {
                try
                {
                    var text = await File.ReadAllTextAsync(PathItemData);
                    text = text.Replace("\r\n", "\n");
                    // Find entries of the form: [ITEM_*] = { ... .price = N,
                    var entryRx = new Regex(@"\[(?<item>ITEM_[A-Z0-9_]+)\]\s*=\s*\{(?<body>[\s\S]*?)\}", RegexOptions.Multiline);
                    foreach (Match em in entryRx.Matches(text))
                    {
                        string item = em.Groups["item"].Value;
                        var body = em.Groups["body"].Value;
                        var pm = Regex.Match(body, @"\.price\s*=\s*(?<p>\d+)");
                        int price = pm.Success ? int.Parse(pm.Groups["p"].Value) : 0;
                        _itemsWithPrices.Add((item, price));
                    }
                    // fallback: sort by enum value using _itemValues if available
                    if (_itemValues.Count > 0)
                        _itemsWithPrices = _itemsWithPrices.OrderBy(t => _itemValues.TryGetValue(t.ItemMacro, out var v) ? v : int.MaxValue).ToList();
                }
                catch { }
            }

            // Parse mart_items.s sections (if present)
            _martSections = new();
            if (HasMartItems && PathMartItems != null)
            {
                try
                {
                    var lines = await File.ReadAllTextAsync(PathMartItems);
                    lines = lines.Replace("\r\n", "\n");
                    var secRx = new Regex(@"^\s*\.org\s+(?<addr>0x[0-9A-Fa-f]+)(?<body>[\s\S]*?)(?=^\s*\.org|\Z)", RegexOptions.Multiline);
                    foreach (Match sm in secRx.Matches(lines))
                    {
                        var addr = sm.Groups["addr"].Value;
                        var body = sm.Groups["body"].Value;
                        string label = string.Empty;
                        // Find descriptive comment: first try above, then a few lines below the .org
                        {
                            bool found = false;
                            int cursor = sm.Index - 1;
                            for (int attempts = 0; attempts < 8 && cursor > 0; attempts++)
                            {
                                int lineStart = lines.LastIndexOf('\n', Math.Max(0, cursor - 1));
                                if (lineStart < 0) lineStart = 0;
                                string line = lines.Substring(lineStart, cursor - lineStart).Trim();
                                if (!string.IsNullOrEmpty(line))
                                {
                                    var m = Regex.Match(line, @"^/\*\s*(?<lbl>[^*/][^{}]*?)\s*\*/\s*$");
                                    if (m.Success &&
                                        !m.Value.Contains("const u16", StringComparison.OrdinalIgnoreCase) &&
                                        !m.Value.Contains("spills over", StringComparison.OrdinalIgnoreCase))
                                    {
                                        label = m.Groups["lbl"].Value.Trim();
                                        found = true;
                                        break;
                                    }
                                }
                                cursor = lineStart;
                            }
                            if (!found)
                            {
                                // Look ahead up to 5 lines for an inline label right after .org
                                int orgLineEnd = lines.IndexOf('\n', sm.Index);
                                if (orgLineEnd < 0) orgLineEnd = sm.Index;
                                int lookCursor = orgLineEnd + 1;
                                for (int attempts = 0; attempts < 5 && lookCursor < lines.Length; attempts++)
                                {
                                    int nextEnd = lines.IndexOf('\n', lookCursor);
                                    if (nextEnd < 0) nextEnd = lines.Length;
                                    string line = lines.Substring(lookCursor, nextEnd - lookCursor).Trim();
                                    if (!string.IsNullOrEmpty(line))
                                    {
                                        var m = Regex.Match(line, @"^/\*\s*(?<lbl>[^*/][^{}]*?)\s*\*/\s*$");
                                        if (m.Success &&
                                            !m.Value.Contains("const u16", StringComparison.OrdinalIgnoreCase) &&
                                            !m.Value.Contains("spills over", StringComparison.OrdinalIgnoreCase))
                                        {
                                            label = m.Groups["lbl"].Value.Trim();
                                            break;
                                        }
                                    }
                                    lookCursor = nextEnd + 1;
                                }
                            }
                        }
                        // Skip sections with no readable shop label
                        if (string.IsNullOrWhiteSpace(label))
                        {
                            // Exclude anonymous technical sections like 0x020FBB08, 0x020FBBEA, etc.
                            // Only include if it is the known General Poké Mart Table (address 0x020FBF22)
                            if (!string.Equals(addr, "0x020FBF22", StringComparison.OrdinalIgnoreCase))
                                continue;
                            label = "General Poké Mart Table";
                        }
                        var sec = new MartSection { Key = addr, Label = label };
                        // General table is at 0x020FBF22 in your template
                        sec.IsGeneralTable = string.Equals(addr, "0x020FBF22", StringComparison.OrdinalIgnoreCase);
                        // Read .halfword pairs item/badge for general table, else single item entries ending with 0xFFFF
                        var hwRx = new Regex(@"\.halfword\s+(?<val>[A-Z0-9_]+|0x[0-9A-Fa-f]+)", RegexOptions.Multiline);
                        var vals = hwRx.Matches(body).Cast<Match>().Select(m => m.Groups["val"].Value).ToList();
                        if (sec.IsGeneralTable)
                        {
                            for (int i = 0; i + 1 < vals.Count; i += 2)
                            {
                                var it = vals[i]; var badge = vals[i + 1];
                                if (it == "0xFFFF") break;
                                // Normalize known badge macros
                                if (badge == "TWO_BADGES" || badge == "FOUR_BADGES" || badge == "SIX_BADGES")
                                {
                                    // keep as-is
                                }
                                else if (badge == "ONE_BADGE" || badge == "THREE_BADGES" || badge == "FIVE_BADGES" || badge == "SEVEN_BADGES" || badge == "EIGHT_BADGES" || badge == "ZERO_BADGES")
                                {
                                    // keep as-is
                                }
                                else
                                {
                                    // fallback if unexpected token appears
                                    badge = "ZERO_BADGES";
                                }
                                sec.Items.Add(new MartItemEntry { Item = it, BadgeMacro = badge });
                            }
                        }
                        else
                        {
                            foreach (var v in vals)
                            {
                                if (v == "0xFFFF") break;
                                if (v.StartsWith("ITEM_", StringComparison.Ordinal)) sec.Items.Add(new MartItemEntry { Item = v });
                            }
                        }
                        _martSections.Add(sec);
                    }
                }
                catch { }
            }
        }

        public static async Task RefreshSpeciesDetailsAsync(int speciesId, string speciesMacro)
        {
            _levelUpMoves = new();
            _evolutions = new();
            _eggMoves = new();
            Overview = null;
            if (ProjectContext.RootPath == null) return;

            // Ensure macro caches exist for dropdowns
            if (_abilityMacros.Count == 0 || _typeMacros.Count == 0 || _eggGroupMacros.Count == 0 || _growthRateMacros.Count == 0)
            {
                await RefreshCachesAsync();
            }

            // 1) Level-up moves from armips/data/levelupdata.s
            var levelupPath = PathLevelUp ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "levelupdata.s");
            if (File.Exists(levelupPath))
            {
                // naive block scan: find `levelup SPECIES_<name>` and read until terminatelearnset
                var text = await File.ReadAllTextAsync(levelupPath);
                var blockStart = $"levelup {speciesMacro}";
                var idx = text.IndexOf(blockStart, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var endIdx = text.IndexOf("terminatelearnset", idx, StringComparison.Ordinal);
                    if (endIdx > idx)
                    {
                        var block = text.Substring(idx, endIdx - idx);
                        var lineRegex = new Regex(@"learnset\s+(?<move>[A-Z0-9_]+),\s*(?<level>\d+)");
                        foreach (Match m in lineRegex.Matches(block))
                        {
                            var moveName = m.Groups["move"].Value.Trim();
                            var level = int.Parse(m.Groups["level"].Value);
                            _levelUpMoves.Add((level, moveName));
                        }
                        _levelUpMoves.Sort((a,b) => a.level.CompareTo(b.level));
                    }
                }
            }

            // 2) Evolutions from armips/data/evodata.s
            var evopath = PathEvo ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "evodata.s");
            if (File.Exists(evopath))
            {
                var text = await File.ReadAllTextAsync(evopath);
                var blockStart = $"evodata {speciesMacro}";
                var idx = text.IndexOf(blockStart, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var endIdx = text.IndexOf("terminateevodata", idx, StringComparison.Ordinal);
                    if (endIdx > idx)
                    {
                        var block = text.Substring(idx, endIdx - idx);
                        var evoRegex = new Regex(@"evolution\s+(?<method>[A-Z0-9_]+),\s*(?<param>\d+),\s*(?<target>[A-Z0-9_]+)");
                        foreach (Match m in evoRegex.Matches(block))
                        {
                            var method = m.Groups["method"].Value;
                            var param = int.Parse(m.Groups["param"].Value);
                            var target = m.Groups["target"].Value;
                            if (method != "EVO_NONE")
                                _evolutions.Add((method, param, target));
                        }
                    }
                }
            }

            // 3) Egg moves from armips/data/eggmoves.s
            var eggpath = PathEgg ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "eggmoves.s");
            if (File.Exists(eggpath))
            {
                var text = await File.ReadAllTextAsync(eggpath);
                var blockStart = $"eggmoveentry {speciesMacro}";
                var idx = text.IndexOf(blockStart, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var nextIdx = text.IndexOf("eggmoveentry ", idx + blockStart.Length, StringComparison.Ordinal);
                    var endIdx = nextIdx >= 0 ? nextIdx : text.Length;
                    var block = text.Substring(idx, endIdx - idx);
                    var eggRegex = new Regex(@"eggmove\s+(?<move>[A-Z0-9_]+)");
                    foreach (Match m in eggRegex.Matches(block))
                    {
                        _eggMoves.Add(m.Groups["move"].Value.Trim());
                    }
                }
            }

            // 0) Overview from armips/data/mondata.s
            var mondataPath = PathMondata ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "mondata.s");
            if (File.Exists(mondataPath))
            {
                var text = await File.ReadAllTextAsync(mondataPath);
                // Header example: mondata SPECIES_BULBASAUR, "Bulbasaur"
                var blockStart = $"mondata {speciesMacro},";
                var idx = text.IndexOf(blockStart, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    // block ends right before next "\n\nmondata " or EOF
                    var nextIdx = text.IndexOf("\n\nmondata ", idx, StringComparison.Ordinal);
                    var endIdx = nextIdx >= 0 ? nextIdx : text.Length;
                    var block = text.Substring(idx, endIdx - idx);

                    // Parse lines
                    SpeciesOverview ov = new();
                    var bs = Regex.Match(block, @"basestats\s+(?<hp>\d+),\s*(?<atk>\d+),\s*(?<def>\d+),\s*(?<spd>\d+),\s*(?<satk>\d+),\s*(?<sdef>\d+)");
                    if (bs.Success)
                    {
                        ov.BaseHp = int.Parse(bs.Groups["hp"].Value);
                        ov.BaseAttack = int.Parse(bs.Groups["atk"].Value);
                        ov.BaseDefense = int.Parse(bs.Groups["def"].Value);
                        ov.BaseSpeed = int.Parse(bs.Groups["spd"].Value);
                        ov.BaseSpAttack = int.Parse(bs.Groups["satk"].Value);
                        ov.BaseSpDefense = int.Parse(bs.Groups["sdef"].Value);
                    }
                    var types = Regex.Match(block, @"types\s+(?<t1>[A-Z0-9_]+),\s*(?<t2>[A-Z0-9_]+)");
                    if (types.Success)
                    {
                        ov.Type1 = types.Groups["t1"].Value;
                        ov.Type2 = types.Groups["t2"].Value;
                    }
                    var catchrate = Regex.Match(block, @"catchrate\s+(?<v>\d+)");
                    if (catchrate.Success) ov.CatchRate = int.Parse(catchrate.Groups["v"].Value);
                    var baseexp = Regex.Match(block, @"baseexp\s+(?<v>\d+)");
                    if (baseexp.Success) ov.BaseExp = int.Parse(baseexp.Groups["v"].Value);
                    var ev = Regex.Match(block, @"evyields\s+(?<hp>\d+),\s*(?<atk>\d+),\s*(?<def>\d+),\s*(?<spd>\d+),\s*(?<satk>\d+),\s*(?<sdef>\d+)");
                    if (ev.Success)
                        ov.EvYields = (
                            int.Parse(ev.Groups["hp"].Value),
                            int.Parse(ev.Groups["atk"].Value),
                            int.Parse(ev.Groups["def"].Value),
                            int.Parse(ev.Groups["spd"].Value),
                            int.Parse(ev.Groups["satk"].Value),
                            int.Parse(ev.Groups["sdef"].Value));
                    var items = Regex.Match(block, @"items\s+(?<i1>[A-Z0-9_]+),\s*(?<i2>[A-Z0-9_]+)");
                    if (items.Success)
                    {
                        ov.Item1 = items.Groups["i1"].Value;
                        ov.Item2 = items.Groups["i2"].Value;
                    }
                    var gender = Regex.Match(block, @"genderratio\s+(?<v>\d+)");
                    if (gender.Success) ov.GenderRatio = int.Parse(gender.Groups["v"].Value);
                    var eggcycles = Regex.Match(block, @"eggcycles\s+(?<v>\d+)");
                    if (eggcycles.Success) ov.EggCycles = int.Parse(eggcycles.Groups["v"].Value);
                    var friendship = Regex.Match(block, @"basefriendship\s+(?<v>\d+)");
                    if (friendship.Success) ov.BaseFriendship = int.Parse(friendship.Groups["v"].Value);
                    var growth = Regex.Match(block, @"growthrate\s+(?<v>[A-Z0-9_]+)");
                    if (growth.Success) ov.GrowthRate = growth.Groups["v"].Value;
                    var egg = Regex.Match(block, @"egggroups\s+(?<e1>[A-Z0-9_]+),\s*(?<e2>[A-Z0-9_]+)");
                    if (egg.Success)
                    {
                        ov.EggGroup1 = egg.Groups["e1"].Value;
                        ov.EggGroup2 = egg.Groups["e2"].Value;
                    }
                    var abilities = Regex.Match(block, @"abilities\s+(?<a1>[A-Z0-9_]+),\s*(?<a2>[A-Z0-9_]+)");
                    if (abilities.Success)
                    {
                        ov.Ability1 = abilities.Groups["a1"].Value;
                        ov.Ability2 = abilities.Groups["a2"].Value;
                    }
                    // Hidden ability from data/HiddenAbilityTable.c
                    try
                    {
                        var hatPath = Path.Combine(ProjectContext.RootPath, "data", "HiddenAbilityTable.c");
                        if (File.Exists(hatPath))
                        {
                            var hat = await File.ReadAllTextAsync(hatPath);
                            var pattern = new Regex(@"\[\s*" + Regex.Escape(speciesMacro) + @"\s*\]\s*=\s*(?<ab>ABILITY_[A-Z0-9_]+)", RegexOptions.Multiline);
                            var mm = pattern.Match(hat);
                            if (mm.Success) ov.AbilityHidden = mm.Groups["ab"].Value;
                        }
                    }
                    catch { }
                    var runChance = Regex.Match(block, @"runchance\s+(?<v>\d+)");
                    if (runChance.Success) ov.RunChance = int.Parse(runChance.Groups["v"].Value);
                    var dexClass = Regex.Match(block, @"mondexclassification\s+[^,]+,\s*""(?<v>[\s\S]*?)""\s*$", RegexOptions.Multiline);
                    if (dexClass.Success) ov.DexClassification = dexClass.Groups["v"].Value;
                    var dexEntry = Regex.Match(block, @"mondexentry\s+[^,]+,\s*""(?<v>[\s\S]*?)""\s*$", RegexOptions.Multiline);
                    if (dexEntry.Success) ov.DexEntry = dexEntry.Groups["v"].Value;
                    var dexHeight = Regex.Match(block, @"mondexheight\s+[^,]+,\s*""(?<v>[\s\S]*?)""\s*$", RegexOptions.Multiline);
                    if (dexHeight.Success) ov.DexHeight = dexHeight.Groups["v"].Value;
                    var dexWeight = Regex.Match(block, @"mondexweight\s+[^,]+,\s*""(?<v>[\s\S]*?)""\s*$", RegexOptions.Multiline);
                    if (dexWeight.Success) ov.DexWeight = dexWeight.Groups["v"].Value;

                    Overview = ov;
                }
            }

            // Always pull BaseExp from BaseExperienceTable.c for display (mondata baseexp is ignored)
            try
            {
                if (Overview != null && ProjectContext.RootPath != null)
                {
                    var baseExpPath = Path.Combine(ProjectContext.RootPath, "data", "BaseExperienceTable.c");
                    if (File.Exists(baseExpPath))
                    {
                        var lines = await File.ReadAllLinesAsync(baseExpPath);
                        var pattern = new Regex(@"\[" + Regex.Escape(speciesMacro) + @"\]\s*=\s*(?<v>\d+)");
                        foreach (var line in lines)
                        {
                            var m = pattern.Match(line);
                            if (m.Success)
                            {
                                Overview.BaseExp = int.Parse(m.Groups["v"].Value);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            // 4) TM/HM headers list for display (needed for the TM/HM tab)
            _tmhmMoves = new();
            if (ProjectContext.RootPath == null) return;
            var tmHeadersPath = Path.Combine(ProjectContext.RootPath!, "armips", "data", "tmlearnset.txt");
            if (File.Exists(tmHeadersPath))
            {
                var content = await File.ReadAllTextAsync(tmHeadersPath);
                content = content.Replace("\r\n", "\n");
                var headerRegex2 = new Regex(@"^(TM|HM)\d{3}:\s+[A-Z0-9_]+\s*$", RegexOptions.Multiline);
                foreach (Match m in headerRegex2.Matches(content))
                {
                    var header = m.Value.Trim();
                    if (!_tmhmMoves.Contains(header)) _tmhmMoves.Add(header);
                }
            }

            // 5) Tutor learnsets from armips/data/tutordata.txt
            _tutorMoves = new();
            _tutorHeaders = new();
            _tutorSelectedForSpecies = new HashSet<string>();
            var tutorPath = PathTutor ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "tutordata.txt");
            if (File.Exists(tutorPath))
            {
                var lines = await File.ReadAllLinesAsync(tutorPath);
                string currentTutor = string.Empty;
                string currentMove = string.Empty;
                int currentCost = 0;
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                    if (line.StartsWith("TUTOR_"))
                    {
                        var m = Regex.Match(line, @"^(?<tutor>TUTOR_[A-Z0-9_]+):\s+(?<move>[A-Z0-9_]+)\s+(?<cost>\d+)");
                        if (m.Success)
                        {
                            currentTutor = m.Groups["tutor"].Value;
                            currentMove = m.Groups["move"].Value;
                            currentCost = int.Parse(m.Groups["cost"].Value);
                            _tutorHeaders.Add((currentTutor, currentMove, currentCost));
                        }
                        continue;
                    }
                    if (line.StartsWith("SPECIES_"))
                    {
                        if (line == speciesMacro)
                        {
                            _tutorMoves.Add((currentTutor, currentMove, currentCost));
                            _tutorSelectedForSpecies.Add($"{currentTutor}: {currentMove} {currentCost}");
                        }
                    }
                }
            }

            // 6) Selected TM/HM headers for this species
            _tmhmSelectedForSpecies = new HashSet<string>();
            var tmMap = PathTm ?? Path.Combine(ProjectContext.RootPath, "armips", "data", "tmlearnset.txt");
            if (File.Exists(tmMap))
            {
                var text = await File.ReadAllTextAsync(tmMap);
                text = text.Replace("\r\n", "\n");
                var headerRegex = new Regex(@"^(TM|HM)\d{3}:\s+[A-Z0-9_]+\s*$", RegexOptions.Multiline);
                var headers = headerRegex.Matches(text).Cast<Match>().ToList();
                for (int i = 0; i < headers.Count; i++)
                {
                    var start = headers[i].Index;
                    var end = (i + 1 < headers.Count) ? headers[i + 1].Index : text.Length;
                    var block = text.Substring(start, end - start);
                    if (block.Contains("\n\t" + speciesMacro + "\n") || block.Contains("\n    " + speciesMacro + "\n") || block.EndsWith("\n\t" + speciesMacro) || block.EndsWith("\n    " + speciesMacro))
                    {
                        _tmhmSelectedForSpecies.Add(headers[i].Value.Trim());
                    }
                }
            }
        }

        // Resolved data file paths (auto-detected)
        public static string? PathLevelUp { get; private set; }
        public static string? PathEvo { get; private set; }
        public static string? PathEgg { get; private set; }
        public static string? PathTutor { get; private set; }
        public static string? PathTm { get; private set; }
        public static string? PathMondata { get; private set; }
        public static string? PathTrainers { get; private set; }

        private static void ResolveDataPaths()
        {
            if (ProjectContext.RootPath == null) return;
            PathLevelUp = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "levelupdata.s"))
                ?? FindFile("levelupdata.s");
            PathEvo = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "evodata.s"))
                ?? FindFile("evodata.s");
            PathEgg = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "eggmoves.s"))
                ?? FindFile("eggmoves.s");
            PathTutor = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "tutordata.txt"))
                ?? FindFile("tutordata.txt");
            PathTm = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "tmlearnset.txt"))
                ?? FindFile("tmlearnset.txt");
            PathMondata = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "mondata.s"))
                ?? FindFile("mondata.s");
            PathTrainers = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "trainers", "trainers.s"))
                ?? FindFile("trainers.s");
            PathEncounters = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "encounters.s"))
                ?? FindFile("encounters.s");
            PathHeadbutt = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "data", "headbutt.s"))
                ?? FindFile("headbutt.s");
            PathItemData = PreferExisting(Path.Combine(ProjectContext.RootPath, "data", "itemdata", "itemdata.c"))
                ?? FindFile("itemdata.c");
            PathMartItems = PreferExisting(Path.Combine(ProjectContext.RootPath, "armips", "asm", "custom", "mart_items.s"))
                ?? FindFile("mart_items.s");
        }

        private static string? PreferExisting(string path)
        {
            return File.Exists(path) ? path : null;
        }

        private static string? FindFile(string fileName)
        {
            try
            {
                if (ProjectContext.RootPath == null) return null;
                foreach (var f in Directory.EnumerateFiles(ProjectContext.RootPath, "*", SearchOption.AllDirectories))
                {
                    if (string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            catch { }
            return null;
        }
    }
}


