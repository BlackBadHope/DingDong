using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;

internal sealed record SharedQuestCompletion(string NpcKey, int QuestId, int Ordinal, string Performer, int Coins, int RewardType, int RewardCount);

public sealed partial class NpcQuestWorldLedger
{
    // Forward compatibility: the current release reads versions 1..2 and drops the whole
    // ledger on anything newer. The tag therefore stays at version 2; the clock, the
    // choices and the per-record Paused/StartClock/Baseline fields are optional keys that
    // older builds ignore and this build reads when present.
    internal const int SharedVersion = 2;
    private const int MaximumReadableVersion = 3;
    internal const int MaximumChoices = 512;
    // Choice id recorded for a Dialogue-Choice stage completed before choices were kept
    // (worlds from the release line before the ledger). It satisfies that stage's
    // choice conditions and counts as neutral in the finale lean.
    internal const string LegacyChoice = "legacy";
    private static bool legacyChoicesPending;
    private static readonly Dictionary<string, ResidentQuestProgress> Shared = new(StringComparer.Ordinal);
    private static readonly List<SharedQuestCompletion> CompletionHistory = new();
    // Dialogue-Choice outcomes, "npcKey:stageIndex" -> choice id. World-wide and permanent
    // (QUEST-FORMS: "the pick is written to the world ledger"); the lore codex reads them.
    private static readonly Dictionary<string, string> ChoiceLedger = new(StringComparer.Ordinal);
    internal static IReadOnlyList<SharedQuestCompletion> History => CompletionHistory;
    internal static IReadOnlyDictionary<string, string> Choices => ChoiceLedger;
    // World quest clock: game ticks scaled by the day rate, advanced only by the world
    // authority. Delayed stages are measured against it, never against a client.
    internal static long Clock { get; private set; }
    // Bumped whenever choices or the clock-visible state change on this machine; the
    // codex cache keys on it together with the player snapshot revision.
    internal static uint WorldRevision { get; private set; }

    internal static ResidentQuestProgress GetShared(string npcKey)
    {
        if (!NpcTownManifest.TryGet(npcKey, out _)) throw new ArgumentException("Unknown resident", nameof(npcKey));
        if (!Shared.TryGetValue(npcKey, out var value)) Shared.Add(npcKey, value = new());
        return value;
    }

    internal static bool TryGetShared(string key, out ResidentQuestProgress value) => Shared.TryGetValue(key, out value);
    internal static IReadOnlyList<(string NpcKey, ResidentQuestProgress Progress)> SharedSnapshot(bool clone = true) =>
        Shared.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (p.Key, clone ? p.Value.Clone() : p.Value)).ToArray();

    internal static string ChoiceKey(string npcKey, int stage) => npcKey + ":" + stage;
    internal static bool TryGetChoice(string npcKey, int stage, out string choiceId) => ChoiceLedger.TryGetValue(ChoiceKey(npcKey, stage), out choiceId);

    internal static void RecordChoice(string npcKey, int stage, string choiceId)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || string.IsNullOrEmpty(choiceId) || !NpcTownManifest.TryGet(npcKey, out _)) return;
        string key = ChoiceKey(npcKey, stage);
        if (!ChoiceLedger.ContainsKey(key) && ChoiceLedger.Count >= MaximumChoices) return;
        ChoiceLedger[key] = choiceId;
        BumpWorldRevision();
    }

    // An abandoned Dialogue-Choice stage drops its pending pick: accepting it again asks anew.
    internal static void ForgetChoice(string npcKey, int stage)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (ChoiceLedger.Remove(ChoiceKey(npcKey, stage))) BumpWorldRevision();
    }

    internal static bool IsLegacyChoice(string choiceId) => string.Equals(choiceId, LegacyChoice, StringComparison.Ordinal);

    // Runs after the world ledger (and any legacy per-player records) are loaded.
    // 1) An active Dialogue-Choice stage counts as answered only with a recorded pick;
    //    otherwise (a world saved by the release line, which kept the pick only in
    //    Progress and clamped it) the stage asks again instead of silently taking option 1.
    // 2) Worlds without a choice ledger get the legacy marker for every Dialogue-Choice
    //    stage already completed, so codex entries and the book history do not stay locked.
    private static void ReconcileLoadedChoices()
    {
        bool backfill = legacyChoicesPending;
        legacyChoicesPending = false;
        foreach (var (npcKey, p) in Shared)
        {
            if (p.Active && p.ActiveKind == ResidentQuestKind.DialogueChoice && p.Progress > 0 && !ChoiceLedger.ContainsKey(ChoiceKey(npcKey, p.ActiveVariant)))
                p.Progress = 0;
            if (!backfill || PersonalResidentQuestCatalog.Story(npcKey) is not { } story) continue;
            for (int stage = 0; stage < Math.Min(p.Completed, story.Steps.Length); stage++)
            {
                string key = ChoiceKey(npcKey, stage);
                if (story.Steps[stage].Kind == "DialogueChoice" && !ChoiceLedger.ContainsKey(key) && ChoiceLedger.Count < MaximumChoices)
                    ChoiceLedger[key] = LegacyChoice;
            }
        }
        BumpWorldRevision();
    }

    private static void BumpWorldRevision() { unchecked { WorldRevision++; } }

    private static void ClearShared() { Shared.Clear(); CompletionHistory.Clear(); MissingClaimTicks.Clear(); ChoiceLedger.Clear(); Clock = 0; BumpWorldRevision(); }

    private static void MigrateLegacyRecords()
    {
        if (Shared.Count != 0) return;
        // Old independent player chains overlap. Preserve their maximum instead
        // of awarding the same chain's reputation more than once.
        foreach (var pair in Records.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            Split(pair.Key, out string owner, out string npc);
            if (Shared.TryGetValue(npc, out var prior) && prior.Completed >= pair.Value.Completed) continue;
            var migrated = pair.Value.Clone();
            migrated.ClearActive(); // Legacy random active tasks retire; reputation remains.
            Shared[npc] = migrated;
        }
    }

    internal static void RecordCompletion(string key, int quest, Player performer, QuestRewardReceipt reward)
    {
        var p = GetShared(key); p.LastCompletedBy = performer.name;
        CompletionHistory.Add(new(key, quest, p.Completed, performer.name, reward.CopperValue, reward.ThemedItemType, reward.ThemedItemCount));
        if (CompletionHistory.Count > 64) CompletionHistory.RemoveRange(0, CompletionHistory.Count - 64);
    }

    // A leaving performer no longer erases shared progress: the task pauses with its
    // progress intact and any player (the same one included) can resume it.
    internal static void ReleaseClaims(string owner)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || owner.Length == 0) return;
        bool changed = false;
        foreach (var p in Shared.Values.Where(p => p.Active && p.IsAssignedTo(owner))) { p.Paused = true; changed = true; }
        if (changed) { PublishShared(); NpcQuestNetwork.SendQuestSnapshot(null,-1); }
    }

    // The returning performer gets back every task still paused under their identity.
    private static bool ResumeClaims(string owner)
    {
        bool changed = false;
        foreach (var p in Shared.Values.Where(p => p.Active && p.IsPausedFor(owner))) { p.Paused = false; changed = true; }
        return changed;
    }

    private static void PublishShared()
    {
        foreach (Player p in Main.ActivePlayers) p.GetModPlayer<NpcQuestPlayer>().Touch();
    }

    internal static void ReplaceNetworkWorldState(long clock, IEnumerable<KeyValuePair<string, string>> choices)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        Clock = clock; ChoiceLedger.Clear();
        foreach (var pair in choices) ChoiceLedger[pair.Key] = pair.Value;
        BumpWorldRevision();
    }

    private static void SaveShared(TagCompound tag)
    {
        tag["SharedResidentQuestVersion"] = SharedVersion;
        tag["SharedResidentQuestClock"] = Clock;
        tag["SharedResidentQuests"] = Shared.Select(pair =>
        {
            var p = pair.Value;
            return new TagCompound {
                ["Npc"] = pair.Key, ["Completed"] = p.Completed, ["Active"] = p.Active,
                ["Progress"] = p.Progress, ["Id"] = p.ActiveQuestId, ["Kind"] = (int)p.ActiveKind,
                ["Variant"] = p.ActiveVariant, ["Item"] = p.ActiveItemType, ["Goal"] = p.ActiveGoal,
                ["Radius"] = p.ActiveRadiusTiles, ["Target"] = p.ActiveTargetKey, ["TargetName"] = p.ActiveTargetName,
                ["Coins"] = p.RewardCopper, ["Difficulty"] = p.DifficultyRank,
                ["Story"] = p.ActiveStoryKey, ["RewardItem"] = p.RewardItemType, ["RewardCount"] = p.RewardItemCount,
                ["Assignee"] = p.AssignedTo, ["AssigneeName"] = p.AssignedName, ["LastCompletedBy"] = p.LastCompletedBy,
                ["Paused"] = p.Paused, ["StartClock"] = p.StartClock, ["Baseline"] = p.Baseline
            };
        }).ToList();
        tag["SharedResidentQuestHistory"] = CompletionHistory.Select(h => new TagCompound {
            ["Npc"] = h.NpcKey, ["Quest"] = h.QuestId, ["Ordinal"] = h.Ordinal, ["By"] = h.Performer,
            ["Coins"] = h.Coins, ["Item"] = h.RewardType, ["Stack"] = h.RewardCount }).ToList();
        tag["SharedResidentQuestChoices"] = ChoiceLedger.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => new TagCompound { ["Key"] = p.Key, ["Id"] = p.Value }).ToList();
    }

    private static void LoadShared(TagCompound tag)
    {
        ClearShared();
        int version = tag.GetInt("SharedResidentQuestVersion");
        legacyChoicesPending = !tag.ContainsKey("SharedResidentQuestChoices");
        if (version is < 1 or > MaximumReadableVersion) return;
        // Worlds from the release line have no clock; it starts at zero and Delayed stages
        // are re-anchored below so their already-earned waiting time is kept.
        bool hasClock = tag.ContainsKey("SharedResidentQuestClock");
        Clock = hasClock ? Math.Max(0L, tag.GetLong("SharedResidentQuestClock")) : 0L;
        foreach (var t in tag.GetList<TagCompound>("SharedResidentQuests"))
        {
            string key = t.GetString("Npc"); if (!NpcTownManifest.TryGet(key, out _)) continue;
            var p = new ResidentQuestProgress {
                Completed = Math.Max(0,t.GetInt("Completed")), Active = t.GetBool("Active"), Progress = Math.Max(0,t.GetInt("Progress")),
                ActiveQuestId = t.GetInt("Id"), ActiveKind = (ResidentQuestKind)t.GetInt("Kind"), ActiveVariant = t.GetInt("Variant"),
                ActiveItemType = t.GetInt("Item"), ActiveGoal = Math.Max(0,t.GetInt("Goal")), ActiveRadiusTiles = Math.Max(0,t.GetInt("Radius")),
                ActiveTargetKey = t.GetString("Target"), ActiveTargetName = t.GetString("TargetName"), RewardCopper = Math.Max(0,t.GetInt("Coins")),
                ActiveStoryKey = t.GetString("Story"), RewardItemType = t.GetInt("RewardItem"), RewardItemCount = t.GetInt("RewardCount"),
                DifficultyRank = Math.Clamp(t.GetInt("Difficulty"),0,3), AssignedTo = t.GetString("Assignee"), AssignedName = t.GetString("AssigneeName"), LastCompletedBy = t.GetString("LastCompletedBy") };
            if (t.ContainsKey("Paused")) p.Paused = t.GetBool("Paused");
            if (t.ContainsKey("StartClock")) p.StartClock = t.GetLong("StartClock");
            if (t.ContainsKey("Baseline")) p.Baseline = Math.Max(0, t.GetInt("Baseline"));
            if (p.Active && (p.ActiveGoal <= 0 || !Enum.IsDefined(p.ActiveKind) || !Guid.TryParseExact(p.AssignedTo,"N",out _))) p.ClearActive();
            if (p.Active && NpcTownManifest.TryGet(key,out var identity)) p.NormalizeLoaded(identity,p.AssignedTo);
            if (!p.Active) p.ClearActive();
            p.Progress = Math.Min(p.Progress,p.ActiveGoal);
            if (p.Active && p.ActiveKind == ResidentQuestKind.Delayed && (!hasClock || !t.ContainsKey("StartClock"))) p.StartClock = Clock - p.Progress;
            Shared[key] = p;
        }
        foreach (var h in tag.GetList<TagCompound>("SharedResidentQuestHistory").TakeLast(64))
            if (NpcTownManifest.TryGet(h.GetString("Npc"),out _)) CompletionHistory.Add(new(h.GetString("Npc"),h.GetInt("Quest"),h.GetInt("Ordinal"),h.GetString("By"),h.GetInt("Coins"),h.GetInt("Item"),h.GetInt("Stack")));
        if (!legacyChoicesPending)
            foreach (var c in tag.GetList<TagCompound>("SharedResidentQuestChoices").Take(MaximumChoices))
            {
                string choiceKey = c.GetString("Key"), id = c.GetString("Id");
                int split = choiceKey.LastIndexOf(':');
                if (split > 0 && id.Length > 0 && id.Length <= 64 && NpcTownManifest.TryGet(choiceKey[..split], out _)) ChoiceLedger[choiceKey] = id;
            }
        BumpWorldRevision();
    }
}
