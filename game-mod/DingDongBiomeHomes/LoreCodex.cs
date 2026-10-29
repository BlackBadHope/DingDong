using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace DingDongBiomeHomes;

/// <summary>
/// Client-side codex state ("Chronicle"). Every unlock is computed from data each client
/// already holds: the synchronized quest snapshot (Completed of all 61 residents), the
/// world's recorded Dialogue-Choice outcomes and vanilla progression flags. Two clients
/// of one world therefore open exactly the same entries without extra packets. Only
/// "read" marks are personal (BookTrackerPlayer, shared with the book's chronicle).
/// </summary>
internal static class LoreCodex
{
    private static uint playerRevision = uint.MaxValue, worldRevision = uint.MaxValue;
    private static int flagMask = -1, worldId = int.MinValue;
    private static readonly Dictionary<string, int> Completed = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Unlocked = new(StringComparer.Ordinal);
    private static readonly HashSet<string> OpenNodes = new(StringComparer.Ordinal);
    private static string[] unlockedOrdered = Array.Empty<string>();
    internal static int MainStages { get; private set; }
    internal static int TotalStages { get; private set; }
    /// <summary>Stages of the drawn main line in this world (normally 100; less when the pack lacks residents).</summary>
    internal static int MainStagesMax { get; private set; } = ResidentStoryRoles.NominalMainStages;
    internal static int Act { get; private set; } = 1;
    internal static int LeanGardeners { get; private set; }
    internal static int LeanTiders { get; private set; }
    internal static int LeanArchivists { get; private set; }
    /// <summary>"gardeners" | "tiders" | "archivists" | "balance", or "" while the main line is unfinished.</summary>
    internal static string Ending { get; private set; } = "";
    internal static IReadOnlyList<string> UnlockedIds { get { Refresh(); return unlockedOrdered; } }

    internal static void Invalidate() { playerRevision = worldRevision = uint.MaxValue; }

    /// <summary>Cheap when nothing changed; call from Update, never from Draw.</summary>
    internal static void Refresh()
    {
        Player local = Main.gameMenu ? null : Main.LocalPlayer;
        if (local == null || !local.active) return;
        var quest = local.GetModPlayer<NpcQuestPlayer>();
        int flags = FlagMask();
        if (quest.Revision == playerRevision && NpcQuestWorldLedger.WorldRevision == worldRevision && flags == flagMask && Main.worldID == worldId) return;
        playerRevision = quest.Revision; worldRevision = NpcQuestWorldLedger.WorldRevision; flagMask = flags; worldId = Main.worldID;
        Completed.Clear();
        foreach (var (npcKey, progress) in quest.CurrentSnapshot())
            Completed[npcKey] = Math.Clamp(progress.Completed, 0, SafeLength(npcKey));
        var main = ResidentStoryRoles.MainSet(Main.worldID);
        MainStages = main.Sum(k => Completed.GetValueOrDefault(k));
        MainStagesMax = Math.Max(1, main.Sum(k => SafeLength(k)));
        TotalStages = Completed.Values.Sum();
        Act = Math.Max(1, LoreCatalog.ActThresholds.Count(t => MainStages >= MainThreshold(t)));
        int g = 0, t = 0, a = 0;
        foreach (var pair in NpcQuestWorldLedger.Choices)
        {
            int split = pair.Key.LastIndexOf(':');
            if (split <= 0 || !main.Contains(pair.Key[..split]) || !int.TryParse(pair.Key[(split + 1)..], out int stage)) continue;
            // A pick counts once its stage is turned in; legacy markers stay neutral.
            if (Completed.GetValueOrDefault(pair.Key[..split]) <= stage || NpcQuestWorldLedger.IsLegacyChoice(pair.Value)) continue;
            switch (LoreCatalog.Lean(pair.Key[..split], stage, pair.Value))
            {
                case "gardeners": g++; break;
                case "tiders": t++; break;
                case "archivists": a++; break;
            }
        }
        LeanGardeners = g; LeanTiders = t; LeanArchivists = a;
        Ending = MainStages >= MainStagesMax ? Winner(g, t, a) : "";
        Unlocked.Clear();
        foreach (var entry in LoreCatalog.Entries)
            if (entry.Condition != null && Evaluate(entry.Condition)) Unlocked.Add(entry.Id);
        OpenNodes.Clear();
        foreach (var (node, residents) in LoreCatalog.Nodes)
            if (residents.Any(r => Completed.GetValueOrDefault(r) >= 1)) OpenNodes.Add(node);
        unlockedOrdered = LoreCatalog.Entries.Where(e => Unlocked.Contains(e.Id)).Select(e => e.Id).ToArray();
    }

    /// <summary>Act thresholds are authored for a 100-stage main line; a shorter line scales them so the last act stays reachable.</summary>
    internal static int MainThreshold(int authored) => MainThreshold(authored, MainStagesMax);
    internal static int MainThreshold(int authored, int mainTotal) =>
        mainTotal <= 0 || mainTotal >= ResidentStoryRoles.NominalMainStages ? authored : (int)Math.Ceiling(authored * (double)mainTotal / ResidentStoryRoles.NominalMainStages);

    private static int SafeLength(string npcKey)
    {
        try { return PersonalResidentQuestCatalog.Length(npcKey); }
        catch (KeyNotFoundException) { return 5; }
    }

    internal static string Winner(int g, int t, int a)
    {
        int best = Math.Max(g, Math.Max(t, a));
        if (best == 0) return "balance";
        int leaders = (g == best ? 1 : 0) + (t == best ? 1 : 0) + (a == best ? 1 : 0);
        return leaders > 1 ? "balance" : g == best ? "gardeners" : t == best ? "tiders" : "archivists";
    }

    internal static bool IsUnlocked(string entryId) { Refresh(); return Unlocked.Contains(entryId); }
    internal static bool IsNodeOpen(string node) { Refresh(); return OpenNodes.Contains(node); }
    internal static bool IsResidentPageOpen(string npcKey) { Refresh(); return Completed.GetValueOrDefault(npcKey) >= SafeLength(npcKey); }
    internal static bool IsNew(string entryId) => IsUnlocked(entryId) && !Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>().IsRead(entryId);
    internal static int NewCount() { Refresh(); int n = 0; var lore = Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>(); foreach (string id in unlockedOrdered) if (!lore.IsRead(id)) n++; return n; }
    internal static void MarkRead(string entryId) { if (IsUnlocked(entryId)) Main.LocalPlayer.GetModPlayer<BookTrackerPlayer>().MarkRead(entryId); }

    private static bool Evaluate(LoreCond c) => c.Kind switch
    {
        LoreCondKind.Always => true,
        LoreCondKind.MainStages => MainStages >= MainThreshold(c.Count),
        LoreCondKind.Stages => TotalStages >= c.Count,
        LoreCondKind.ForceStages => Completed.Where(p => PersonalResidentQuestCatalog.Force(p.Key) == c.Force).Sum(p => p.Value) >= c.Count,
        LoreCondKind.Resident => Completed.GetValueOrDefault(c.NpcKey) >= c.Stage,
        LoreCondKind.Choice => Completed.GetValueOrDefault(c.NpcKey) > c.Stage && NpcQuestWorldLedger.TryGetChoice(c.NpcKey, c.Stage, out string id) && (id == c.Id || NpcQuestWorldLedger.IsLegacyChoice(id)),
        LoreCondKind.Lean => Ending.Length > 0 && Ending == c.Value,
        LoreCondKind.Flag => Flag(c.Value),
        LoreCondKind.All => c.Children.All(Evaluate),
        LoreCondKind.Any => c.Children.Any(Evaluate),
        _ => false
    };

    internal static bool Flag(string name) => name switch
    {
        "hardMode" => Main.hardMode,
        "downedBoss1" => NPC.downedBoss1,
        "downedBoss2" => NPC.downedBoss2,
        "downedBoss3" => NPC.downedBoss3,
        "downedQueenBee" => NPC.downedQueenBee,
        "downedMechBossAny" => NPC.downedMechBossAny,
        "downedPlantBoss" => NPC.downedPlantBoss,
        "downedGolemBoss" => NPC.downedGolemBoss,
        "downedAncientCultist" => NPC.downedAncientCultist,
        "downedMoonlord" => NPC.downedMoonlord,
        _ => false
    };

    private static int FlagMask()
    {
        int mask = 0;
        for (int i = 0; i < LoreCatalog.Flags.Length; i++) if (Flag(LoreCatalog.Flags[i])) mask |= 1 << i;
        return mask;
    }
}
