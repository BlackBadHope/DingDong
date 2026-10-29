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
	// The lifecycle resident walks its whole personal story (every stage: accept -> objective
	// -> turn in -> inventory reward -> ledger), then must refuse a further stage. Every other
	// resident completes its first stage. The ledger then round-trips through a save tag.
	internal static string RunProbe(Mod mod, string worldFile)
	{
		List<string> failures;
		NpcQuestWorldLedger ledger;
		Player player;
		NPC npc;
		NpcTownIdentity identity;
		NpcQuestPlayer questPlayer;
		int accepted, completed, gated, rewards, residentsAccepted, residentsCompleted, residentRewards;
		bool storyComplete, journalAccepted, journalReady;
		InitializeLifecycleProbe(out failures, out ledger, out player, out npc, out identity, out questPlayer, out accepted, out completed, out gated, out rewards, out storyComplete, out journalAccepted, out journalReady, out residentsAccepted, out residentsCompleted, out residentRewards);
		int stages = StoryLength(identity.NpcKey);
		Expect(failures, "story-length", stages > 0);
		for (int cycle = 0; cycle < stages; cycle++)
		{
			InteractWithSyntheticResident(player, npc);
			ResidentQuestProgress progress = questPlayer.Get(identity.NpcKey);
			Expect(failures, $"accept-{cycle + 1}", progress.Active && progress.Completed == cycle && progress.ActiveVariant == cycle && progress.ActiveGoal > 0 && progress.RewardCopper > 0);
			if (!progress.Active)
				break;
			accepted++;
			QuestJournalSnapshot acceptedSnapshot = QuestJournalContent.Build(player);
			journalAccepted |= acceptedSnapshot.Entries.Any(entry => !entry.Ready);
			int rewardCopper, rewardDifficulty;
			QuestJournalSnapshot readySnapshot;
			ResidentQuestDefinition definition = progress.Definition(identity);
			gated = AdvanceObjectiveAndVerifyGate(failures, player, npc, identity, questPlayer, gated, cycle, progress, out rewardCopper, out rewardDifficulty, out readySnapshot);
			journalReady |= readySnapshot.Entries.Any(entry => entry.Ready);
			bool rewardNow = TurnInAndVerifyReward(player, npc, questPlayer, definition, rewardCopper, rewardDifficulty, out string rewardDetail);
			ResidentQuestProgress turnedIn = questPlayer.Get(identity.NpcKey);
			VerifyRewardAndLedger(failures, identity, ref completed, ref rewards, cycle, turnedIn, rewardNow, rewardDetail);
			foreach (Item item in player.inventory)
				item.TurnToAir();
		}

		RunAllResidents(player, npc, questPlayer, identity.NpcKey, failures, ref residentsAccepted, ref residentsCompleted, ref residentRewards, ref storyComplete);
		TagCompound persisted = new();
		ledger.SaveWorldData(persisted);
		Expect(failures, "ledger-version<=2", persisted.GetInt("SharedResidentQuestVersion") <= 2);
		questPlayer.ReplaceCurrentSnapshot(Array.Empty<(string NpcKey, ResidentQuestProgress Progress)>());
		ledger.LoadWorldData(persisted);
		NpcQuestWorldLedger.BindPlayer(player, allowLocalImport: false);
		bool persistence = NpcTownManifest.All.All(candidate =>
		{
			ResidentQuestProgress reloaded = questPlayer.Get(candidate.NpcKey);
			return !reloaded.Active && reloaded.Completed == ExpectedCompleted(candidate.NpcKey, identity.NpcKey);
		});
		string summary = BuildLifecycleProbeSummary(mod, worldFile, failures, player, npc, stages, accepted, completed, gated, rewards, residentsAccepted, residentsCompleted, residentRewards, storyComplete, journalAccepted, journalReady, persistence);
		player.active = false;
		npc.active = false;
		return summary;
	}

	internal const string LifecycleResidentKey = "Terraria/Guide";

	// The lifecycle resident finished its whole story; every other resident its first stage.
	internal static int ExpectedCompleted(string npcKey, string lifecycleKey = LifecycleResidentKey) =>
		string.Equals(npcKey, lifecycleKey, StringComparison.Ordinal) ? StoryLength(npcKey) : 1;
}
