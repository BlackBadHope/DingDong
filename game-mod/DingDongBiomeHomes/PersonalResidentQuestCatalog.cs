using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal sealed class ResidentStory
{
    public string NpcKey { get; set; } = "";
    public string Force { get; set; } = "";
    public ResidentStoryStep[] Steps { get; set; } = Array.Empty<ResidentStoryStep>();
}

internal sealed class ResidentStoryChoice
{
    public string Id { get; set; } = "";
    public string TextRu { get; set; } = "";
    public string TextEn { get; set; } = "";
    internal string Text => Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName == "ru" ? TextRu : TextEn;
}

internal sealed class ResidentStoryStep
{
    public string TitleRu { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Target { get; set; } = "";
    public int Count { get; set; }
    public string Reward { get; set; } = "";
    public int RewardCount { get; set; }
    public int Coins { get; set; }
    // First Word Was forms (all optional; validated per Kind in EnsureLoaded).
    public string NpcTarget { get; set; } = "";
    public int Seconds { get; set; }
    public string TileToken { get; set; } = "";
    public string ForbiddenDebuff { get; set; } = "";
    public int Days { get; set; }
    public ResidentStoryChoice[] Choices { get; set; } = Array.Empty<ResidentStoryChoice>();
    public string SourceTag { get; set; } = "";
    internal string Key;
    internal int ItemType, RewardType, TargetNpcType, TileType, BuffType;
    internal int[] Alternatives = Array.Empty<int>();
    internal string Title => Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName == "ru" ? TitleRu : TitleEn;
}

internal static class PersonalResidentQuestCatalog
{
    internal const int FirstId = 100_000;
    internal const int ObserveRadiusTiles = 30, GatherRadiusTiles = 40, TicksPerDay = 86_400, MaxDelayedDays = 3;
    private static readonly Dictionary<string, ResidentStory> Stories = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ResidentStoryStep> Steps = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> TileAliases = new(StringComparer.Ordinal)
    { ["Torch"] = "Torches", ["TreeSapling"] = "Saplings", ["Planter"] = "PlanterBox" };
    private static bool loaded;
    internal static void Reset() { Stories.Clear(); Steps.Clear(); loaded = false; }
    internal static void EnsureLoaded()
    {
        if (loaded) return;
        Stories.Clear(); Steps.Clear();
        var rows = JsonSerializer.Deserialize<ResidentStory[]>(DingDongBiomeHomes.Instance.GetFileBytes("Quests/ResidentStories.json"));
        foreach (var row in rows ?? Array.Empty<ResidentStory>())
        {
            if (!NpcTownManifest.TryGet(row.NpcKey, out _) || row.Steps.Length < 3 || row.Steps.Length > 12 || !Stories.TryAdd(row.NpcKey, row))
                throw new InvalidOperationException("Invalid or duplicate resident story: " + row.NpcKey);
            for (int i = 0; i < row.Steps.Length; i++)
            {
                var s = row.Steps[i]; s.Key = row.NpcKey + ":" + i;
                bool counted = s.Kind is "Deliver" or "Hunt" or "Visit" or "GatherEnv" or "Trial" or "Expedition";
                if (s.TitleRu.Length == 0 || s.TitleEn.Length == 0 || counted && s.Count <= 0 || s.RewardCount <= 0 || s.Coins <= 0)
                    throw new InvalidOperationException("Incomplete personal quest: " + s.Key);
                s.RewardType = ResolveItem(s.Reward);
                switch (s.Kind)
                {
                    case "Deliver": s.Alternatives = s.Target.Split('|').Select(ResolveItem).ToArray(); s.ItemType = s.Alternatives[0]; break;
                    case "Expedition":
                        if (!s.SourceTag.StartsWith("dingdong:acquisition/", StringComparison.Ordinal)) throw new InvalidOperationException("Expedition needs an acquisition tag: " + s.Key);
                        s.Alternatives = s.Target.Split('|').Select(ResolveItem).ToArray(); s.ItemType = s.Alternatives[0]; break;
                    case "Hunt": s.TargetNpcType = ResolveNpc(s.Target); break;
                    case "Trial": s.TargetNpcType = ResolveNpc(s.NpcTarget); s.BuffType = ResolveBuff(s.ForbiddenDebuff); break;
                    case "Observe":
                        if (s.Seconds is < 5 or > 600) throw new InvalidOperationException("Observe seconds out of range: " + s.Key);
                        s.TargetNpcType = ResolveNpc(s.NpcTarget); break;
                    case "GatherEnv": s.TileType = ResolveTile(s.TileToken); break;
                    case "Delayed":
                        if (s.Days < 1 || s.Days > MaxDelayedDays) throw new InvalidOperationException("Delayed days out of range: " + s.Key); break;
                    case "DialogueChoice":
                        if (s.Choices.Length is < 2 or > 3 || s.Choices.Any(c => c.Id.Length == 0 || c.TextRu.Length == 0 || c.TextEn.Length == 0) || s.Choices.Select(c => c.Id).Distinct().Count() != s.Choices.Length)
                            throw new InvalidOperationException("Dialogue choice needs 2-3 distinct options: " + s.Key);
                        break;
                    case "Visit": if (!Enum.TryParse<HomeBiome>(s.Target, out _)) throw new InvalidOperationException("Invalid personal quest target: " + s.Key); break;
                    default: throw new InvalidOperationException("Unknown personal quest kind: " + s.Key);
                }
                Steps.Add(s.Key, s);
            }
        }
        if (Stories.Count != NpcTownManifest.AuthoredCount) throw new InvalidOperationException("Every resident requires an authored story.");
        loaded = true;
    }
    private static int ResolveItem(string key)
    {
        if (ItemID.Search.TryGetId(key, out int type) && type > 0) return type;
        if (ModContent.TryFind<ModItem>(key, out var item)) return item.Type;
        throw new InvalidOperationException("Unknown resident quest item: " + key);
    }
    private static int ResolveNpc(string key)
    {
        if (key.StartsWith("Terraria/", StringComparison.Ordinal) && NPCID.Search.TryGetId(key[9..], out int type) && type > 0) return type;
        if (ModContent.TryFind<ModNPC>(key, out var npc)) return npc.Type;
        throw new InvalidOperationException("Unknown resident quest creature: " + key);
    }
    private static int ResolveBuff(string key)
    {
        if (BuffID.Search.TryGetId(key, out int type) && type > 0) return type;
        if (ModContent.TryFind<ModBuff>(key, out var buff)) return buff.Type;
        throw new InvalidOperationException("Unknown resident quest debuff: " + key);
    }
    private static int ResolveTile(string token)
    {
        string key = TileAliases.TryGetValue(token, out string alias) ? alias : token;
        if (TileID.Search.TryGetId(key, out int type) && type >= 0) return type;
        if (ModContent.TryFind<ModTile>(token, out var tile)) return tile.Type;
        throw new InvalidOperationException("Unknown resident quest tile: " + token);
    }
    internal static int Length(string npc) { EnsureLoaded(); return Stories[npc].Steps.Length; }
    internal static bool Complete(string npc, int completed) => completed >= Length(npc);
    internal static string Force(string npc) { EnsureLoaded(); return Stories.TryGetValue(npc, out var s) ? s.Force : ""; }
    internal static ResidentStoryStep Step(string key) { EnsureLoaded(); return Steps.TryGetValue(key, out var s) ? s : null; }
    internal static ResidentStoryStep Step(string npc, int stage) => Step(npc + ":" + stage);
    internal static ResidentStory Story(string npc) { EnsureLoaded(); return Stories.TryGetValue(npc, out var s) ? s : null; }
    internal static string NpcContentKey(int npcType) => npcType < NPCID.Count ? "Terraria/" + NPCID.Search.GetName(npcType) : NPCLoader.GetNPC(npcType)?.FullName ?? "";
    internal static ResidentQuestDefinition Resolve(NpcTownIdentity npc, int chapter)
    {
        EnsureLoaded(); chapter = Math.Clamp(chapter, 0, Length(npc.NpcKey) - 1); var s = Stories[npc.NpcKey].Steps[chapter];
        ResidentQuestKind kind = s.Kind switch
        {
            "Deliver" => ResidentQuestKind.DeliverSupplies, "Hunt" => ResidentQuestKind.CreatureHunt, "Observe" => ResidentQuestKind.Observe,
            "GatherEnv" => ResidentQuestKind.GatherEnv, "Trial" => ResidentQuestKind.Trial, "DialogueChoice" => ResidentQuestKind.DialogueChoice,
            "Delayed" => ResidentQuestKind.Delayed, "Expedition" => ResidentQuestKind.Expedition, _ => ResidentQuestKind.VisitBiome
        };
        int goal = kind switch
        {
            ResidentQuestKind.Observe => s.Seconds * 60, ResidentQuestKind.Delayed => s.Days * TicksPerDay,
            ResidentQuestKind.DialogueChoice => 1, _ => s.Count
        };
        int radius = kind switch { ResidentQuestKind.Observe => ObserveRadiusTiles, ResidentQuestKind.GatherEnv => GatherRadiusTiles, _ => 0 };
        string targetKey = kind switch
        {
            ResidentQuestKind.Observe or ResidentQuestKind.Trial => NpcContentKey(s.TargetNpcType),
            ResidentQuestKind.GatherEnv => "tile:" + s.TileToken, ResidentQuestKind.Expedition => s.SourceTag,
            ResidentQuestKind.DialogueChoice => "choice", ResidentQuestKind.Delayed => "days:" + s.Days, _ => s.Target
        };
        string targetName = kind switch
        {
            ResidentQuestKind.CreatureHunt or ResidentQuestKind.Observe or ResidentQuestKind.Trial => Lang.GetNPCNameValue(s.TargetNpcType),
            ResidentQuestKind.DeliverSupplies or ResidentQuestKind.Expedition => string.Join(" / ", s.Alternatives.Select(Lang.GetItemNameValue)),
            ResidentQuestKind.GatherEnv => Language.GetTextValue("Mods.DingDongBiomeHomes.SharedQuests.Tiles." + s.TileToken),
            ResidentQuestKind.DialogueChoice => s.Title, ResidentQuestKind.Delayed => s.Days.ToString(),
            _ => Language.GetTextValue("Mods.DingDongBiomeHomes.Hub.Biomes." + s.Target)
        };
        return new(FirstId + chapter, kind, chapter, s.ItemType, goal, radius, targetKey, targetName, s.Coins, s.Key, s.RewardType, s.RewardCount);
    }
    /// <summary>A repeat of a finished chain cycles its stages; dialogue choices and waits are the
    /// world's one-time answers, so those stages are skipped.</summary>
    internal static ResidentQuestDefinition ResolveRepeat(NpcTownIdentity npc, int completed)
    {
        EnsureLoaded(); var steps = Stories[npc.NpcKey].Steps; int length = steps.Length;
        for (int i = 0; i < length; i++)
        {
            int chapter = (completed + i) % length;
            if (steps[chapter].Kind is not ("DialogueChoice" or "Delayed")) return Resolve(npc, chapter);
        }
        return Resolve(npc, completed % length);
    }
    internal static bool TryResolve(string key, out ResidentQuestDefinition definition)
    {
        var step = Step(key);
        if (step != null)
        {
            int split = key.LastIndexOf(':');
            if (NpcTownManifest.TryGet(key[..split], out var npc) && int.TryParse(key[(split+1)..], out int chapter))
            { definition = Resolve(npc, chapter); return true; }
        }
        definition = default; return false;
    }
    internal static bool ContractPass(out string failure)
    {
        try
        {
            EnsureLoaded();
            foreach (var npc in NpcTownManifest.All)
            {
                var definitions = Enumerable.Range(0, Length(npc.NpcKey)).Select(i => Resolve(npc, i)).ToArray();
                if (definitions.Any(d => !d.IsValid || d.RewardItemType <= 0 || d.RewardItemCount <= 0) || definitions.Select(d => d.StoryKey).Distinct().Count() != definitions.Length)
                    throw new InvalidOperationException("Invalid personal quest chain: " + npc.NpcKey);
                if (ResidentTrustPolicy.RequiredQuests(npc) > definitions.Length) throw new InvalidOperationException("Unreachable reputation: " + npc.NpcKey);
            }
            failure = ""; return true;
        }
        catch (Exception ex) { failure = ex.Message; return false; }
    }
}

public sealed class PersonalResidentQuestContentSystem : ModSystem
{
    public override void Unload() => PersonalResidentQuestCatalog.Reset();
}
