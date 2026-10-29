using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

// Data contract "dingdong-lore-v1" (Quests/Lore/<node>.json + Quests/Lore/codex.json).
// Texts are ru/en pairs in JSON, worded on the client only; packets carry keys.
internal sealed class LoreText
{
    [JsonPropertyName("ru")] public string Ru { get; set; } = "";
    [JsonPropertyName("en")] public string En { get; set; } = "";
}

internal sealed class LorePage
{
    [JsonPropertyName("title")] public LoreText Title { get; set; }
    [JsonPropertyName("body")] public LoreText Body { get; set; }
}

internal sealed class LoreChoice
{
    [JsonPropertyName("outcome")] public LoreText Outcome { get; set; }
    [JsonPropertyName("lean")] public string Lean { get; set; } = "";
}

internal sealed class LoreStage
{
    [JsonPropertyName("index")] public int Index { get; set; } = -1;
    [JsonPropertyName("titleRu")] public string TitleRu { get; set; } = "";
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("description")] public LoreText Description { get; set; }
    [JsonPropertyName("offer")] public LoreText Offer { get; set; }
    [JsonPropertyName("complete")] public LoreText Complete { get; set; }
    [JsonPropertyName("waiting")] public LoreText Waiting { get; set; }
    [JsonPropertyName("choices")] public Dictionary<string, LoreChoice> Choices { get; set; }
}

internal sealed class LoreResident
{
    [JsonPropertyName("npcKey")] public string NpcKey { get; set; } = "";
    [JsonPropertyName("force")] public string Force { get; set; } = "";
    [JsonPropertyName("epithet")] public LoreText Epithet { get; set; }
    [JsonPropertyName("quote")] public LoreText Quote { get; set; }
    [JsonPropertyName("intro")] public LoreText Intro { get; set; }
    [JsonPropertyName("findHint")] public LoreText FindHint { get; set; }
    [JsonPropertyName("epilogue")] public LoreText Epilogue { get; set; }
    [JsonPropertyName("stages")] public LoreStage[] Stages { get; set; } = Array.Empty<LoreStage>();
}

internal sealed class LoreNodeFile
{
    [JsonPropertyName("format")] public string Format { get; set; } = "";
    [JsonPropertyName("node")] public string Node { get; set; } = "";
    [JsonPropertyName("nodePage")] public LorePage NodePage { get; set; }
    [JsonPropertyName("residents")] public LoreResident[] Residents { get; set; } = Array.Empty<LoreResident>();
}

internal sealed class LoreChapter
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("order")] public int Order { get; set; }
    [JsonPropertyName("title")] public LoreText Title { get; set; }
}

internal sealed class LoreEntry
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("chapter")] public string Chapter { get; set; } = "";
    [JsonPropertyName("order")] public int Order { get; set; }
    [JsonPropertyName("icon")] public string Icon { get; set; } = "";
    [JsonPropertyName("title")] public LoreText Title { get; set; }
    [JsonPropertyName("body")] public LoreText Body { get; set; }
    [JsonPropertyName("lockedHint")] public LoreText LockedHint { get; set; }
    [JsonPropertyName("unlock")] public JsonElement Unlock { get; set; }
    [JsonIgnore] internal LoreCond Condition;
    [JsonIgnore] internal int IconItemType;
}

internal sealed class LoreCodexFile
{
    [JsonPropertyName("format")] public string Format { get; set; } = "";
    [JsonPropertyName("chapters")] public LoreChapter[] Chapters { get; set; } = Array.Empty<LoreChapter>();
    [JsonPropertyName("entries")] public LoreEntry[] Entries { get; set; } = Array.Empty<LoreEntry>();
}

internal enum LoreCondKind : byte { Always, MainStages, Stages, ForceStages, Resident, Choice, Lean, Flag, All, Any }

internal sealed class LoreCond
{
    internal LoreCondKind Kind;
    internal int Count, Stage;
    internal string Force = "", NpcKey = "", Id = "", Value = "";
    internal LoreCond[] Children = Array.Empty<LoreCond>();

    // Order-independent canonical form, used by the strict contract for the required entries.
    internal string Canonical() => Kind switch
    {
        LoreCondKind.Always => "always",
        LoreCondKind.MainStages => "mainStages:" + Count,
        LoreCondKind.Stages => "stages:" + Count,
        LoreCondKind.ForceStages => "forceStages:" + Force + ":" + Count,
        LoreCondKind.Resident => "resident:" + NpcKey + ":" + Stage,
        LoreCondKind.Choice => "choice:" + NpcKey + ":" + Stage + ":" + Id,
        LoreCondKind.Lean => "lean:" + Value,
        LoreCondKind.Flag => "flag:" + Value,
        _ => (Kind == LoreCondKind.All ? "all[" : "any[") + string.Join(",", Children.Select(c => c.Canonical()).OrderBy(c => c, StringComparer.Ordinal)) + "]"
    };
}

internal static class LoreCatalog
{
    internal const string NodeFormat = "dingdong-lore-node-v1", CodexFormat = "dingdong-lore-codex-v1";
    internal static readonly string[] Forces = { "gardeners", "tiders", "archivists", "tricksters", "unassigned" };
    internal static readonly string[] Leans = { "gardeners", "tiders", "archivists", "none" };
    internal static readonly string[] EndingLeans = { "gardeners", "tiders", "archivists", "balance" };
    internal static readonly string[] Flags = { "hardMode", "downedBoss1", "downedBoss2", "downedBoss3", "downedQueenBee", "downedMechBossAny", "downedPlantBoss", "downedGolemBoss", "downedAncientCultist", "downedMoonlord" };
    internal static readonly string[] CodexChapters = { "world", "forces", "arcs", "finale" };
    internal const string PlacesChapter = "places";
    // Node membership is fixed by Narrative/FirstWordWas/scenarios/<node>-node.json.
    internal static readonly (string Node, string[] Residents)[] Nodes =
    {
        ("town", new[] { "Terraria/Guide", "Terraria/Merchant", "Terraria/Nurse", "Terraria/ArmsDealer", "Terraria/Demolitionist", "Terraria/GoblinTinkerer", "Terraria/Mechanic" }),
        ("arts", new[] { "Terraria/Painter", "Terraria/Stylist", "Terraria/PartyGirl", "Terraria/DyeTrader", "Terraria/Clothier", "Terraria/DD2Bartender" }),
        ("nature", new[] { "Terraria/Dryad", "Terraria/WitchDoctor", "Terraria/Truffle", "Terraria/Angler", "Terraria/Golfer", "Terraria/BestiaryGirl" }),
        ("wanderers", new[] { "Terraria/Pirate", "Terraria/SantaClaus", "Terraria/TaxCollector", "Terraria/Steampunker", "Terraria/Cyborg", "Terraria/Wizard", "Terraria/Princess" }),
        ("ocean", new[] { "CalamityMod/SeaKing", "ThoriumMod/Diverman", "FishermanNPC/Fisherman", "UnuBattleRods/Fishylady", "CalValEX/JellyPriestNPC" }),
        ("alchemist", new[] { "AlchemistNPCLite/Alchemist", "AlchemistNPCLite/Brewer", "AlchemistNPCLite/Jeweler", "AlchemistNPCLite/Operator", "AlchemistNPCLite/Architect", "AlchemistNPCLite/Musician", "AlchemistNPCLite/Tinkerer", "AlchemistNPCLite/YoungBrewer" }),
        ("calamity", new[] { "CalamityMod/Archmage", "CalamityMod/Bandit", "CalamityMod/BrimstoneWitch", "CalValEX/Jharim", "CalValEX/OracleNPC", "miningcracks_take_on_luiafk/Deeps", "WeaponEnchantments/Witch" }),
        ("craftsmen", new[] { "ThoriumMod/Blacksmith", "ThoriumMod/Cobbler", "ThoriumMod/Cook", "ThoriumMod/Tracker", "ThoriumMod/DesertAcolyte" }),
        ("fargo", new[] { "Fargowiltas/Mutant", "Fargowiltas/Deviantt", "Fargowiltas/Abominationn", "Fargowiltas/LumberJack", "Fargowiltas/Squirrel" }),
        ("thorium", new[] { "ThoriumMod/ConfusedZombie", "ThoriumMod/WeaponMaster", "ThoriumMod/Druid", "ThoriumMod/Spiritualist", "MagicStorage/Golem" })
    };
    // Required codex entries and their exact unlock conditions (canonical form).
    internal static readonly (string Id, string Condition)[] RequiredEntries =
    {
        ("world.first-word", "always"), ("world.empty-houses", "always"), ("world.edges", "stages:10"),
        ("world.ocean-unspoken", "any[flag:downedBoss3,resident:CalamityMod/SeaKing:2,resident:ThoriumMod/Diverman:2]"),
        ("world.second-phrase", "flag:hardMode"), ("world.moon-seal", "flag:downedMoonlord"),
        ("forces.gardeners", "forceStages:gardeners:3"), ("forces.tiders", "forceStages:tiders:3"), ("forces.archivists", "forceStages:archivists:3"),
        ("forces.tricksters", "forceStages:tricksters:2"), ("forces.personal", "forceStages:unassigned:2"),
        ("arcs.act1", "always"), ("arcs.act2", "mainStages:15"), ("arcs.act3", "mainStages:40"), ("arcs.act4", "mainStages:70"), ("arcs.act5", "mainStages:100"),
        ("finale.gardeners", "lean:gardeners"), ("finale.tiders", "lean:tiders"), ("finale.archivists", "lean:archivists"), ("finale.balance", "lean:balance")
    };
    internal static readonly int[] ActThresholds = { 0, 15, 40, 70, 100 };

    private static readonly Dictionary<string, LoreResident> ResidentsByKey = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> NodeByResident = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, LoreNodeFile> NodeFiles = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, LoreEntry> EntriesById = new(StringComparer.Ordinal);
    private static readonly List<string> LoadWarnings = new();
    private static LoreCodexFile codex;
    private static LoreEntry[] orderedEntries = Array.Empty<LoreEntry>();
    private static bool loaded;
    private static readonly Regex EntryId = new("^[a-z]+\\.[a-z0-9-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex Token = new("\\{[^}]*\\}", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = false, ReadCommentHandling = JsonCommentHandling.Disallow };

    internal static IReadOnlyList<string> Warnings { get { EnsureLoaded(); return LoadWarnings; } }
    internal static bool IsRu => Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName == "ru";

    internal static void Reset()
    {
        ResidentsByKey.Clear(); NodeByResident.Clear(); NodeFiles.Clear(); EntriesById.Clear(); LoadWarnings.Clear();
        codex = null; orderedEntries = Array.Empty<LoreEntry>(); loaded = false;
    }

    // Lenient load: a missing or broken file only produces warnings; the mod always loads.
    internal static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        foreach (var (node, residents) in Nodes)
        {
            foreach (string key in residents) NodeByResident[key] = node;
            LoreNodeFile file = Read<LoreNodeFile>(node);
            if (file == null) continue;
            NodeFiles[node] = file;
            foreach (var resident in file.Residents ?? Array.Empty<LoreResident>())
            {
                if (resident?.NpcKey == null || resident.NpcKey.Length == 0) { LoadWarnings.Add(node + ": resident without npcKey"); continue; }
                if (!ResidentsByKey.TryAdd(resident.NpcKey, resident)) LoadWarnings.Add(node + ": duplicate " + resident.NpcKey);
                resident.Stages ??= Array.Empty<LoreStage>();
            }
        }
        codex = Read<LoreCodexFile>("codex");
        if (codex != null)
        {
            codex.Chapters ??= Array.Empty<LoreChapter>(); codex.Entries ??= Array.Empty<LoreEntry>();
            foreach (var entry in codex.Entries)
            {
                if (entry?.Id == null || entry.Id.Length == 0 || !EntriesById.TryAdd(entry.Id, entry)) { LoadWarnings.Add("codex: missing or duplicate entry id " + entry?.Id); continue; }
                var errors = new List<string>();
                entry.Condition = ParseCondition(entry.Unlock, errors, entry.Id);
                if (entry.Condition == null) { LoadWarnings.Add("codex: " + entry.Id + " unlock " + string.Join(";", errors)); entry.Condition = new LoreCond { Kind = LoreCondKind.Flag, Value = "never" }; }
                entry.IconItemType = ResolveItem(entry.Icon);
            }
            orderedEntries = codex.Entries.Where(e => e?.Id != null && EntriesById.TryGetValue(e.Id, out var same) && ReferenceEquals(same, e))
                .OrderBy(e => ChapterOrder(e.Chapter)).ThenBy(e => e.Order).ThenBy(e => e.Id, StringComparer.Ordinal).ToArray();
        }
        foreach (string warning in LoadWarnings.Take(40)) DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_LORE_WARN|" + warning);
    }

    private static T Read<T>(string name) where T : class
    {
        string path = "Quests/Lore/" + name + ".json";
        try
        {
            var mod = DingDongBiomeHomes.Instance;
            if (mod == null || !mod.FileExists(path)) { LoadWarnings.Add(path + ": missing"); return null; }
            return JsonSerializer.Deserialize<T>(mod.GetFileBytes(path), Options);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            LoadWarnings.Add(path + ": " + ex.GetType().Name + " " + ex.Message);
            return null;
        }
    }

    internal static int ResolveItem(string key)
    {
        if (string.IsNullOrEmpty(key)) return ItemID.Book;
        if (ItemID.Search.TryGetId(key, out int type) && type > 0) return type;
        if (key.Contains('/') && ModContent.TryFind<ModItem>(key, out var item)) return item.Type;
        return 0;
    }

    // ---- text access (client) ----
    internal static string Pick(LoreText text)
    {
        if (text == null) return "";
        string primary = IsRu ? text.Ru : text.En, fallback = IsRu ? text.En : text.Ru;
        return !string.IsNullOrEmpty(primary) ? primary : fallback ?? "";
    }

    internal static string Format(LoreText text, string npcKey)
    {
        string raw = Pick(text);
        if (raw.Length == 0 || raw.IndexOf('{') < 0) return raw;
        string player = Main.LocalPlayer?.name ?? "";
        string resident = npcKey != null && npcKey.Length > 0 ? NpcQuestSystem.ResidentName(npcKey) : "";
        return raw.Replace("{player}", player, StringComparison.Ordinal).Replace("{resident}", resident, StringComparison.Ordinal);
    }

    internal static LoreResident Resident(string npcKey) { EnsureLoaded(); return npcKey != null && ResidentsByKey.TryGetValue(npcKey, out var r) ? r : null; }
    internal static LoreStage Stage(string npcKey, int stage)
    {
        var r = Resident(npcKey);
        if (r == null || stage < 0) return null;
        foreach (var s in r.Stages) if (s != null && s.Index == stage) return s;
        return null;
    }
    internal static bool HasResident(string npcKey) => Resident(npcKey) != null;
    internal static string Epithet(string npcKey) => Format(Resident(npcKey)?.Epithet, npcKey);
    internal static string Quote(string npcKey) => Format(Resident(npcKey)?.Quote, npcKey);
    internal static string Intro(string npcKey) => Format(Resident(npcKey)?.Intro, npcKey);
    internal static string FindHint(string npcKey) => Format(Resident(npcKey)?.FindHint, npcKey);
    internal static string Epilogue(string npcKey) => Format(Resident(npcKey)?.Epilogue, npcKey);
    internal static string Description(string npcKey, int stage) => Format(Stage(npcKey, stage)?.Description, npcKey);
    internal static string Offer(string npcKey, int stage) => Format(Stage(npcKey, stage)?.Offer, npcKey);
    internal static string Complete(string npcKey, int stage) => Format(Stage(npcKey, stage)?.Complete, npcKey);
    internal static string Waiting(string npcKey, int stage) => Format(Stage(npcKey, stage)?.Waiting, npcKey);
    internal static string Outcome(string npcKey, int stage, string choiceId) =>
        choiceId != null && Stage(npcKey, stage)?.Choices is { } map && map.TryGetValue(choiceId, out var c) ? Format(c?.Outcome, npcKey) : "";
    internal static string Lean(string npcKey, int stage, string choiceId) =>
        choiceId != null && Stage(npcKey, stage)?.Choices is { } map && map.TryGetValue(choiceId, out var c) && c?.Lean != null ? c.Lean : "none";

    internal static string NodeOf(string npcKey) { EnsureLoaded(); return NodeByResident.GetValueOrDefault(npcKey ?? "", ""); }
    internal static IEnumerable<string> NodeResidents(string node) => Nodes.FirstOrDefault(n => n.Node == node).Residents ?? Array.Empty<string>();
    internal static bool HasNodePage(string node) { EnsureLoaded(); return NodeFiles.TryGetValue(node, out var f) && f.NodePage != null; }
    internal static string NodeTitle(string node) { EnsureLoaded(); return NodeFiles.TryGetValue(node, out var f) ? Format(f.NodePage?.Title, null) : ""; }
    internal static string NodeBody(string node) { EnsureLoaded(); return NodeFiles.TryGetValue(node, out var f) ? Format(f.NodePage?.Body, null) : ""; }

    internal static IReadOnlyList<LoreEntry> Entries { get { EnsureLoaded(); return orderedEntries; } }
    internal static LoreEntry Entry(string id) { EnsureLoaded(); return id != null && EntriesById.TryGetValue(id, out var e) ? e : null; }
    internal static IEnumerable<LoreEntry> EntriesIn(string chapter) => Entries.Where(e => e.Chapter == chapter);
    internal static int ChapterOrder(string chapter)
    {
        var c = codex?.Chapters?.FirstOrDefault(x => x?.Id == chapter);
        return c != null ? c.Order : chapter == PlacesChapter ? 1000 : 500;
    }
    // Chapter ids in display order; "places" is always present (it is filled from node pages).
    internal static IReadOnlyList<string> ChapterIds()
    {
        EnsureLoaded();
        var ids = new List<string>(CodexChapters);
        ids.Add(PlacesChapter);
        return ids.OrderBy(ChapterOrder).ToArray();
    }
    internal static string ChapterTitle(string chapter)
    {
        EnsureLoaded();
        var c = codex?.Chapters?.FirstOrDefault(x => x?.Id == chapter);
        string authored = c != null ? Format(c.Title, null) : "";
        return authored.Length > 0 ? authored : Language.GetTextValue("Mods.DingDongBiomeHomes.QuestEngine.Codex.Chapters." + chapter);
    }

    // ---- unlock conditions ----
    internal static LoreCond ParseCondition(JsonElement e, List<string> errors, string path)
    {
        if (e.ValueKind != JsonValueKind.Object) { errors.Add(path + ": condition must be an object"); return null; }
        var props = e.EnumerateObject().ToArray();
        if (props.Length != 1) { errors.Add(path + ": condition needs exactly one key"); return null; }
        var p = props[0]; var v = p.Value;
        try
        {
            switch (p.Name)
            {
                case "always":
                    if (v.ValueKind != JsonValueKind.True) { errors.Add(path + ": always must be true"); return null; }
                    return new LoreCond { Kind = LoreCondKind.Always };
                case "mainStages": return new LoreCond { Kind = LoreCondKind.MainStages, Count = v.GetInt32() };
                case "stages": return new LoreCond { Kind = LoreCondKind.Stages, Count = v.GetInt32() };
                case "forceStages": return new LoreCond { Kind = LoreCondKind.ForceStages, Force = v.GetProperty("force").GetString() ?? "", Count = v.GetProperty("count").GetInt32() };
                case "resident": return new LoreCond { Kind = LoreCondKind.Resident, NpcKey = v.GetProperty("npcKey").GetString() ?? "", Stage = v.GetProperty("stage").GetInt32() };
                case "choice": return new LoreCond { Kind = LoreCondKind.Choice, NpcKey = v.GetProperty("npcKey").GetString() ?? "", Stage = v.GetProperty("stage").GetInt32(), Id = v.GetProperty("id").GetString() ?? "" };
                case "lean": return new LoreCond { Kind = LoreCondKind.Lean, Value = v.GetString() ?? "" };
                case "flag": return new LoreCond { Kind = LoreCondKind.Flag, Value = v.GetString() ?? "" };
                case "all":
                case "any":
                    if (v.ValueKind != JsonValueKind.Array) { errors.Add(path + ": " + p.Name + " must be an array"); return null; }
                    var children = new List<LoreCond>();
                    int i = 0;
                    foreach (var child in v.EnumerateArray())
                    {
                        var parsed = ParseCondition(child, errors, path + "." + p.Name + "[" + i++ + "]");
                        if (parsed == null) return null;
                        children.Add(parsed);
                    }
                    return new LoreCond { Kind = p.Name == "all" ? LoreCondKind.All : LoreCondKind.Any, Children = children.ToArray() };
                default: errors.Add(path + ": unknown condition " + p.Name); return null;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or FormatException)
        {
            errors.Add(path + ": malformed " + p.Name); return null;
        }
    }

    private static void CheckFeasible(LoreCond c, List<string> errors, string path)
    {
        switch (c.Kind)
        {
            case LoreCondKind.MainStages: if (c.Count is < 0 or > ResidentStoryRoles.MainCount * 5) errors.Add(path + ": mainStages out of range"); break;
            case LoreCondKind.Stages: if (c.Count < 0 || c.Count > NpcTownManifest.AuthoredCount * 5) errors.Add(path + ": stages out of range"); break;
            case LoreCondKind.ForceStages:
                int members = NpcTownManifest.All.Count(n => PersonalResidentQuestCatalog.Force(n.NpcKey) == c.Force);
                if (!Forces.Contains(c.Force) || c.Count < 0 || c.Count > members * 5) errors.Add(path + ": forceStages unreachable");
                break;
            case LoreCondKind.Resident:
                if (!NpcTownManifest.TryGet(c.NpcKey, out _) || c.Stage < 1 || c.Stage > 5) errors.Add(path + ": resident condition unreachable");
                break;
            case LoreCondKind.Choice:
                var step = NpcTownManifest.TryGet(c.NpcKey, out _) ? PersonalResidentQuestCatalog.Step(c.NpcKey, c.Stage) : null;
                if (step == null || step.Kind != "DialogueChoice" || step.Choices.All(x => x.Id != c.Id)) errors.Add(path + ": choice condition references no authored choice");
                break;
            case LoreCondKind.Lean: if (!EndingLeans.Contains(c.Value)) errors.Add(path + ": unknown lean " + c.Value); break;
            case LoreCondKind.Flag: if (!Flags.Contains(c.Value)) errors.Add(path + ": unknown flag " + c.Value); break;
            case LoreCondKind.All:
            case LoreCondKind.Any:
                if (c.Children.Length == 0) errors.Add(path + ": empty " + c.Kind);
                for (int i = 0; i < c.Children.Length; i++) CheckFeasible(c.Children[i], errors, path + "[" + i + "]");
                break;
        }
    }

    // ---- strict contract (mirrors Tools/Quests/check_lore.py) ----
    internal static bool ContractPass(out string failure, out int residents, out int stages, out int codexEntries)
    {
        var errors = new List<string>();
        residents = stages = codexEntries = 0;
        try
        {
            EnsureLoaded();
            foreach (var (node, members) in Nodes)
            {
                if (!NodeFiles.TryGetValue(node, out var file)) { errors.Add(node + ": file missing or unreadable"); continue; }
                if (file.Format != NodeFormat) errors.Add(node + ": format");
                if (file.Node != node) errors.Add(node + ": node name");
                CheckText(file.NodePage?.Title, 1, 40, node + ".nodePage.title", errors);
                CheckText(file.NodePage?.Body, 300, 900, node + ".nodePage.body", errors);
                var listed = (file.Residents ?? Array.Empty<LoreResident>()).Where(r => r != null).Select(r => r.NpcKey).ToArray();
                foreach (string key in listed.Where(k => !members.Contains(k))) errors.Add(node + ": foreign resident " + key);
                foreach (string key in members.Where(k => listed.Count(x => x == k) != 1)) errors.Add(node + ": resident must appear once " + key);
            }
            var stories = NpcTownManifest.All.Select(n => n.NpcKey).ToArray();
            foreach (string key in stories)
            {
                var story = PersonalResidentQuestCatalog.Story(key);
                var lore = Resident(key);
                if (story == null || lore == null) { errors.Add(key + ": lore missing"); continue; }
                int before = errors.Count;
                if (lore.Force != story.Force) errors.Add(key + ": force " + lore.Force + " != " + story.Force);
                CheckText(lore.Epithet, 1, 40, key + ".epithet", errors);
                CheckText(lore.Quote, 1, 110, key + ".quote", errors);
                CheckText(lore.Intro, 200, 480, key + ".intro", errors);
                if (lore.FindHint != null) CheckText(lore.FindHint, 1, 200, key + ".findHint", errors);
                CheckText(lore.Epilogue, 150, 400, key + ".epilogue", errors);
                if (lore.Stages.Length != story.Steps.Length) errors.Add(key + ": stages " + lore.Stages.Length + " != " + story.Steps.Length);
                for (int i = 0; i < Math.Min(lore.Stages.Length, story.Steps.Length); i++)
                {
                    var s = lore.Stages[i]; var step = story.Steps[i]; string path = key + ":" + i;
                    int stageBefore = errors.Count;
                    if (s == null) { errors.Add(path + ": null stage"); continue; }
                    if (s.Index != i || s.TitleRu != step.TitleRu || s.Kind != step.Kind) errors.Add(path + ": index/titleRu/kind misaligned");
                    CheckText(s.Description, 80, 220, path + ".description", errors);
                    CheckText(s.Offer, 1, 160, path + ".offer", errors);
                    CheckText(s.Complete, 1, 160, path + ".complete", errors);
                    if (s.Offer != null && (CountToken(s.Offer.Ru, "{player}") > 1 || CountToken(s.Offer.En, "{player}") > 1)) errors.Add(path + ".offer: {player} more than once");
                    bool delayed = step.Kind == "Delayed", choice = step.Kind == "DialogueChoice";
                    if (delayed != (s.Waiting != null)) errors.Add(path + ": waiting must exist exactly for Delayed");
                    if (s.Waiting != null) CheckText(s.Waiting, 1, 120, path + ".waiting", errors);
                    if (choice != (s.Choices != null)) errors.Add(path + ": choices must exist exactly for DialogueChoice");
                    if (s.Choices != null)
                    {
                        var ids = step.Choices.Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).ToArray();
                        if (!s.Choices.Keys.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(ids)) errors.Add(path + ": choice ids differ from ResidentStories");
                        foreach (var pair in s.Choices)
                        {
                            if (pair.Value == null) { errors.Add(path + ".choices." + pair.Key + ": null"); continue; }
                            CheckText(pair.Value.Outcome, 1, 160, path + ".choices." + pair.Key + ".outcome", errors);
                            if (!Leans.Contains(pair.Value.Lean)) errors.Add(path + ".choices." + pair.Key + ": lean " + pair.Value.Lean);
                        }
                    }
                    if (errors.Count == stageBefore) stages++;
                }
                if (errors.Count == before) residents++;
            }
            if (codex == null) errors.Add("codex: file missing or unreadable");
            else
            {
                if (codex.Format != CodexFormat) errors.Add("codex: format");
                var chapterIds = codex.Chapters.Where(c => c != null).Select(c => c.Id).ToArray();
                if (chapterIds.Distinct().Count() != chapterIds.Length) errors.Add("codex: duplicate chapter");
                foreach (string id in CodexChapters) if (!chapterIds.Contains(id)) errors.Add("codex: chapter missing " + id);
                foreach (var c in codex.Chapters.Where(c => c != null))
                {
                    if (!CodexChapters.Contains(c.Id) && c.Id != PlacesChapter) errors.Add("codex: unknown chapter " + c.Id);
                    CheckText(c.Title, 1, 30, "codex.chapter." + c.Id, errors);
                }
                var rawIds = codex.Entries.Select(e => e?.Id ?? "").ToArray();
                if (rawIds.Distinct(StringComparer.Ordinal).Count() != rawIds.Length) errors.Add("codex: duplicate entry id");
                int always = 0;
                foreach (var e in codex.Entries.Where(e => e != null))
                {
                    int before = errors.Count;
                    if (!EntryId.IsMatch(e.Id ?? "") || !e.Id.StartsWith(e.Chapter + ".", StringComparison.Ordinal) || !CodexChapters.Contains(e.Chapter)) errors.Add(e.Id + ": id/chapter");
                    if (e.IconItemType <= 0) errors.Add(e.Id + ": icon does not resolve " + e.Icon);
                    CheckText(e.Title, 1, 40, e.Id + ".title", errors);
                    CheckText(e.Body, 300, 1200, e.Id + ".body", errors);
                    CheckText(e.LockedHint, 1, 120, e.Id + ".lockedHint", errors);
                    var parseErrors = new List<string>();
                    var cond = ParseCondition(e.Unlock, parseErrors, e.Id);
                    if (cond == null) errors.AddRange(parseErrors);
                    else { CheckFeasible(cond, errors, e.Id); if (cond.Kind == LoreCondKind.Always) always++; }
                    if (errors.Count == before) codexEntries++;
                }
                foreach (var (id, condition) in RequiredEntries)
                {
                    var e = Entry(id);
                    var parsed = e == null ? null : ParseCondition(e.Unlock, new List<string>(), id);
                    if (e == null) errors.Add("codex: required entry missing " + id);
                    else if (parsed == null || parsed.Canonical() != condition) errors.Add("codex: " + id + " unlock must be " + condition);
                }
                int Count(string chapter) => codex.Entries.Count(e => e?.Chapter == chapter);
                if (codex.Entries.Length < 30) errors.Add("codex: fewer than 30 entries");
                if (Count("world") < 6) errors.Add("codex: world needs 6+");
                if (Count("forces") != 5) errors.Add("codex: forces must be 5");
                if (Count("arcs") != 5) errors.Add("codex: arcs must be 5");
                if (Count("finale") != 4) errors.Add("codex: finale must be 4");
                if (always < 3) errors.Add("codex: fewer than 3 always entries");
            }
        }
        catch (Exception ex)
        {
            errors.Add("exception " + ex.GetType().Name + ": " + ex.Message);
        }
        failure = errors.Count == 0 ? "" : string.Join(";", errors.Take(12)) + (errors.Count > 12 ? ";+" + (errors.Count - 12) : "");
        return errors.Count == 0;
    }

    private static int CountToken(string s, string token)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int n = 0, at = 0;
        while ((at = s.IndexOf(token, at, StringComparison.Ordinal)) >= 0) { n++; at += token.Length; }
        return n;
    }

    internal static void CheckText(LoreText t, int min, int max, string path, List<string> errors)
    {
        if (t == null || string.IsNullOrWhiteSpace(t.Ru) || string.IsNullOrWhiteSpace(t.En)) { errors.Add(path + ": ru/en required"); return; }
        if (t.Ru.Length < min || t.Ru.Length > max) errors.Add(path + ": ru length " + t.Ru.Length + " outside " + min + ".." + max);
        if (t.En.Length > (int)Math.Ceiling(max * 1.2)) errors.Add(path + ": en length " + t.En.Length + " over " + (int)Math.Ceiling(max * 1.2));
        foreach (string s in new[] { t.Ru, t.En })
        {
            foreach (Match m in Token.Matches(s))
                if (m.Value is not ("{player}" or "{resident}")) errors.Add(path + ": token " + m.Value);
            if (s.Contains('\t') || s.Contains("[c/", StringComparison.Ordinal) || s.Contains("[i:", StringComparison.Ordinal) || s.Replace("{player}", "").Replace("{resident}", "").IndexOfAny(new[] { '{', '}' }) >= 0)
                errors.Add(path + ": forbidden markup");
        }
        int letters = 0, cyrillic = 0;
        foreach (char ch in Token.Replace(t.Ru, ""))
            if (char.IsLetter(ch)) { letters++; if (ch >= (char)0x0400 && ch <= (char)0x04FF) cyrillic++; }
        if (letters > 0 && cyrillic * 100 < letters * 85) errors.Add(path + ": ru is not Russian enough");
        if (string.Equals(t.Ru.Trim(), t.En.Trim(), StringComparison.Ordinal)) errors.Add(path + ": ru == en");
    }
}

public sealed class LoreCatalogSystem : ModSystem
{
    public override void PostSetupContent()
    {
        bool stories = PersonalResidentQuestCatalog.ContractPass(out string storyFailure);
        string rolesFailure = "";
        bool roles;
        try { ResidentStoryRoles.RunChecks(); roles = true; }
        catch (Exception ex) { roles = false; rolesFailure = ex.Message; }
        bool lore = LoreCatalog.ContractPass(out string loreFailure, out int residents, out int stages, out int codex);
        bool pass = stories && roles && lore;
        string failures = string.Join("/", new[] { storyFailure, rolesFailure, loreFailure }.Where(f => f.Length > 0)).Replace('|', '/').Replace('\r', ' ').Replace('\n', ' ');
        string line = $"DINGDONG_LORE|residents={residents}|stages={stages}|codex={codex}|stories={(stories ? "PASS" : "FAIL")}|roles={(roles ? "PASS" : "FAIL")}|lore={(lore ? "PASS" : "FAIL")}|warnings={LoreCatalog.Warnings.Count}|verdict={(pass ? "PASS" : "FAIL")}|failures={(failures.Length == 0 ? "none" : failures)}";
        if (pass) Mod.Logger.Info(line); else Mod.Logger.Warn(line);
    }

    public override void Unload() => LoreCatalog.Reset();
}
