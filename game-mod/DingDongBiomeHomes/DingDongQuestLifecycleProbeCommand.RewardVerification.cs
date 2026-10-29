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
	private static void VerifyRewardAndLedger(List<string> failures, NpcTownIdentity identity, ref int completed, ref int rewards, int cycle, ResidentQuestProgress turnedIn, bool rewardNow, string rewardDetail)
	{
		bool completedNow = !turnedIn.Active && turnedIn.Completed == cycle + 1;
		Expect(failures, $"complete-{cycle + 1}", completedNow);
		Expect(failures, $"reward-{cycle + 1}({rewardDetail})", rewardNow);
		Expect(failures, $"ledger-{cycle + 1}", NpcQuestWorldLedger.TryRead(ProbeIdentity, identity.NpcKey, out ResidentQuestProgress ledgerProgress) && !ledgerProgress.Active && ledgerProgress.Completed == cycle + 1);
		if (completedNow)
			completed++;
		if (rewardNow)
			rewards++;
	}

	private static int AdvanceObjectiveAndVerifyGate(List<string> failures, Player player, NPC npc, NpcTownIdentity identity, NpcQuestPlayer questPlayer, int gated, int cycle, ResidentQuestProgress progress, out int rewardCopper, out int rewardDifficulty, out QuestJournalSnapshot readySnapshot)
	{
		rewardCopper = progress.RewardCopper;
		rewardDifficulty = progress.DifficultyRank;
		ResidentQuestDefinition definition = progress.Definition(identity);
		if (cycle == 0)
		{
			// An unready stage pays nothing: neither into the inventory nor into the world.
			(long Coins, int Items) heldBefore = Holdings(player, definition.RewardItemType);
			bool[] itemsBeforeGate = CaptureActiveItems();
			InteractWithSyntheticResident(player, npc);
			ResidentQuestProgress gatedProgress = questPlayer.Get(identity.NpcKey);
			List<Item> prematureDrops = NewWorldItems(itemsBeforeGate);
			(long Coins, int Items) heldAfter = Holdings(player, definition.RewardItemType);
			if (gatedProgress.Active && gatedProgress.Completed == 0 && prematureDrops.Count == 0 && heldAfter == heldBefore)
				gated++;
			else
				failures.Add("premature-turn-in");
			foreach (Item item in prematureDrops)
				item.TurnToAir();
		}

		PrepareObjective(player, progress, definition);
		NpcQuestWorldLedger.Record(player, identity.NpcKey, progress);
		Expect(failures, $"ready-{cycle + 1}", NpcQuestSystem.IsReady(player, definition, progress));
		readySnapshot = QuestJournalContent.Build(player);
		return gated;
	}
}
