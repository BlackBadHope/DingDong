using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class DingDongQuestLifecycleProbeCommand
{
	private static void VerifyRewardAndLedger(List<string> failures, NpcTownIdentity identity, ref int completed, ref int rewards, ref bool milestoneBonus, int cycle, int rewardCopper, ResidentQuestProgress turnedIn, List<Item> rewardDrops, long coinDelta, int cacheDelta)
	{
		int expectedCacheDelta = cycle == 4 ? 2 : 1;
		Expect(failures, $"complete-{cycle + 1}", !turnedIn.Active && turnedIn.Completed == cycle + 1);
		Expect(failures, $"coins-{cycle + 1}", coinDelta == rewardCopper);
		Expect(failures, $"cache-{cycle + 1}", cacheDelta == expectedCacheDelta);
		Expect(failures, $"ledger-{cycle + 1}", NpcQuestWorldLedger.TryRead(ProbeIdentity, identity.NpcKey, out ResidentQuestProgress ledgerProgress) && !ledgerProgress.Active && ledgerProgress.Completed == cycle + 1);
		if (!turnedIn.Active && turnedIn.Completed == cycle + 1)
			completed++;
		if (coinDelta == rewardCopper && cacheDelta == expectedCacheDelta)
			rewards++;
		milestoneBonus |= cycle == 4 && cacheDelta == 2;
		foreach (Item item in rewardDrops)
			item.TurnToAir();
	}

	private static int AdvanceObjectiveAndVerifyGate(List<string> failures, Player player, NPC npc, NpcTownIdentity identity, NpcQuestPlayer questPlayer, int gated, int cycle, ResidentQuestProgress progress, out int rewardCopper, out int rewardDifficulty, out QuestJournalSnapshot readySnapshot)
	{
		rewardCopper = progress.RewardCopper;
		rewardDifficulty = progress.DifficultyRank;
		if (cycle == 0)
		{
			bool[] itemsBeforeGate = CaptureActiveItems();
			InteractWithSyntheticResident(player, npc);
			ResidentQuestProgress gatedProgress = questPlayer.Get(identity.NpcKey);
			List<Item> prematureDrops = NewWorldItems(itemsBeforeGate);
			if (gatedProgress.Active && gatedProgress.Completed == 0 && prematureDrops.Count == 0)
				gated++;
			else
				failures.Add("premature-turn-in");
			foreach (Item item in prematureDrops)
				item.TurnToAir();
		}

		ResidentQuestDefinition definition = progress.Definition(identity);
		PrepareObjective(player, progress, definition);
		NpcQuestWorldLedger.Record(player, identity.NpcKey, progress);
		Expect(failures, $"ready-{cycle + 1}", NpcQuestSystem.IsReady(player, definition, progress));
		readySnapshot = QuestJournalContent.Build(player);
		return gated;
	}
}
