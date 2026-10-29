using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
namespace DingDongBiomeHomes;
public sealed partial class NpcQuestWorldLedger
{
    private const int OfflinePauseTicks = 3600;
    private static readonly Dictionary<string,int> MissingClaimTicks = new(StringComparer.Ordinal);
    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        // The world clock runs with the world (sundials and day-rate changes included).
        Clock += Math.Max(1L, (long)Main.dayRate);
        if (Main.GameUpdateCount % 60 != 0) return;
        bool changed = AdvanceDelayed();
        var online = new HashSet<string>(StringComparer.Ordinal); foreach(var player in Main.ActivePlayers) { var quest = player.GetModPlayer<NpcQuestPlayer>(); if(quest.IdentityConfirmed) online.Add(quest.PersistentId); }
        if(online.Count>0)
            foreach(var pair in Shared)
            {
                var p=pair.Value;
                if(!p.Active || p.Paused || online.Contains(p.AssignedTo)) { MissingClaimTicks.Remove(pair.Key); continue; }
                int ticks = MissingClaimTicks.GetValueOrDefault(pair.Key)+60;
                if(ticks<OfflinePauseTicks) { MissingClaimTicks[pair.Key]=ticks; continue; }
                // Performer gone without a clean disconnect: pause, never erase progress.
                p.Paused = true; MissingClaimTicks.Remove(pair.Key); changed=true;
            }
        if(changed) { PublishShared(); NpcQuestNetwork.SendQuestSnapshot(null,-1); }
    }

    // Delayed stages close on the world clock (QUEST-FORMS "Delayed": the timer is the
    // world's, not a client's), whether or not the performer is online.
    private static bool AdvanceDelayed()
    {
        bool changed = false;
        foreach (var pair in Shared)
        {
            var p = pair.Value;
            if (!p.Active || p.ActiveKind != ResidentQuestKind.Delayed || p.ActiveGoal <= 0) continue;
            int next = (int)Math.Clamp(Clock - p.StartClock, 0L, p.ActiveGoal);
            if (next == p.Progress) continue;
            int day = PersonalResidentQuestCatalog.TicksPerDay;
            bool visible = next / day != p.Progress / day || next >= p.ActiveGoal;
            p.Progress = next;
            if (!visible) continue;
            changed = true;
            if (next >= p.ActiveGoal && TryFindOnline(p.AssignedTo, out Player performer))
                QuestNotice.Send(performer, QuestNoticeKind.ObjectiveReady, pair.Key, p.ActiveVariant);
        }
        return changed;
    }

    // Probe hook: moves the world quest clock forward and applies Delayed progress at once.
    internal static void AdvanceClockForProbe(long ticks)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || ticks <= 0) return;
        Clock += ticks;
        if (AdvanceDelayed()) PublishShared();
    }

    internal static bool TryFindOnline(string persistentId, out Player player)
    {
        foreach (Player candidate in Main.ActivePlayers)
        {
            var quest = candidate.GetModPlayer<NpcQuestPlayer>();
            if (quest.IdentityConfirmed && string.Equals(quest.PersistentId, persistentId, StringComparison.Ordinal)) { player = candidate; return true; }
        }
        player = null; return false;
    }

    internal static void ReplaceNetworkHistory(IEnumerable<SharedQuestCompletion> history)
    { if(Main.netMode!=NetmodeID.MultiplayerClient)return; CompletionHistory.Clear();CompletionHistory.AddRange(history.TakeLast(64)); }
}
