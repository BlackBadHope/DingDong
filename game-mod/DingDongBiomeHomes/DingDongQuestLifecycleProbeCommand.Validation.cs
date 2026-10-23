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
	internal static string RunProbe(Mod mod, string worldFile)
	{
		List<string> failures;
		NpcQuestWorldLedger ledger;
		Player player;
		NPC npc;
		NpcTownIdentity identity;
		NpcQuestPlayer questPlayer;
		int accepted, completed, gated, rewards, residentsAccepted, residentsCompleted, residentRewards;
		bool milestoneBonus, journalAccepted, journalReady;
		InitializeLifecycleProbe(out failures, out ledger, out player, out npc, out identity, out questPlayer, out accepted, out completed, out gated, out rewards, out milestoneBonus, out journalAccepted, out journalReady, out residentsAccepted, out residentsCompleted, out residentRewards);
		for (int cycle = 0; cycle < 5; cycle++)
		{
			InteractWithSyntheticResident(player, npc);
			ResidentQuestProgress progress = questPlayer.Get(identity.NpcKey);
			Expect(failures, $"accept-{cycle + 1}", progress.Active && progress.Completed == cycle && progress.ActiveGoal > 0 && progress.RewardCopper > 0);
			if (!progress.Active)
				break;
			accepted++;
			QuestJournalSnapshot acceptedSnapshot = QuestJournalContent.Build(player);
			journalAccepted |= acceptedSnapshot.Entries.Any(entry => !entry.Ready);
			int rewardCopper, rewardDifficulty;
			QuestJournalSnapshot readySnapshot;
			gated = AdvanceObjectiveAndVerifyGate(failures, player, npc, identity, questPlayer, gated, cycle, progress, out rewardCopper, out rewardDifficulty, out readySnapshot);
			journalReady |= readySnapshot.Entries.Any(entry => entry.Ready);
			int cacheType = QuestRewardBalance.CacheItemType(rewardDifficulty);
			bool[] itemsBeforeReward = CaptureActiveItems();
			InteractWithSyntheticResident(player, npc);
			ResidentQuestProgress turnedIn = questPlayer.Get(identity.NpcKey);
			List<Item> rewardDrops = NewWorldItems(itemsBeforeReward);
			long coinDelta = CoinValue(rewardDrops);
			int cacheDelta = rewardDrops.Where(item => item.type == cacheType).Sum(item => item.stack);
			VerifyRewardAndLedger(failures, identity, ref completed, ref rewards, ref milestoneBonus, cycle, rewardCopper, turnedIn, rewardDrops, coinDelta, cacheDelta);
		}

		RunAllResidents(player, npc, questPlayer, failures, ref residentsAccepted, ref residentsCompleted, ref residentRewards);
		TagCompound persisted = new();
		ledger.SaveWorldData(persisted);
		questPlayer.ReplaceCurrentSnapshot(Array.Empty<(string NpcKey, ResidentQuestProgress Progress)>());
		ledger.LoadWorldData(persisted);
		NpcQuestWorldLedger.BindPlayer(player, allowLocalImport: false);
		bool persistence = NpcTownManifest.All.All(candidate =>
		{
			ResidentQuestProgress reloaded = questPlayer.Get(candidate.NpcKey);
			int expectedCompleted = string.Equals(candidate.NpcKey, identity.NpcKey, StringComparison.Ordinal) ? 6 : 1;
			return !reloaded.Active && reloaded.Completed == expectedCompleted;
		});
		string summary = BuildLifecycleProbeSummary(mod, worldFile, failures, player, npc, accepted, completed, gated, rewards, residentsAccepted, residentsCompleted, residentRewards, milestoneBonus, journalAccepted, journalReady, persistence);
		player.active = false;
		npc.active = false;
		return summary;
	}
}
