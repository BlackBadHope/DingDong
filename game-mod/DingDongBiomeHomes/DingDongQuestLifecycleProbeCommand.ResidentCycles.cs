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
	private static void RunAllResidents(Player player, NPC npc, NpcQuestPlayer questPlayer, List<string> failures, ref int accepted, ref int completed, ref int rewards)
	{
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!NpcResidentRegistry.TryGetType(identity.NpcKey, out int npcType) || !SettlementWorldState.TryGetResidence(identity.NpcKey, out SettlementResidenceReport residence))
			{
				failures.Add($"resident-unavailable:{identity.NpcKey}");
				continue;
			}

			foreach (Item item in player.inventory)
				item.TurnToAir();
			Vector2 center = new(residence.Area.Center.X * 16f, residence.Area.Center.Y * 16f);
			player.position = center;
			SetNpcDefaultsForEarlyProbe(npc, npcType, player);
			npc.whoAmI = SyntheticNpcIndex;
			npc.active = true;
			npc.position = center + new Vector2(32f, 0f);
			ResidentQuestProgress before = questPlayer.Get(identity.NpcKey);
			int completedBefore = before.Completed;
			InteractWithSyntheticResident(player, npc);
			ResidentQuestProgress progress = questPlayer.Get(identity.NpcKey);
			bool acceptedNow = progress.Active && progress.Completed == completedBefore && progress.ActiveGoal > 0;
			Expect(failures, $"resident-accept:{identity.NpcKey}", acceptedNow);
			if (!acceptedNow)
				continue;
			accepted++;
			ResidentQuestDefinition definition = progress.Definition(identity);
			int rewardCopper = progress.RewardCopper;
			int rewardDifficulty = progress.DifficultyRank;
			PrepareObjective(player, progress, definition);
			NpcQuestWorldLedger.Record(player, identity.NpcKey, progress);
			bool[] itemsBeforeReward = CaptureActiveItems();
			InteractWithSyntheticResident(player, npc);
			ResidentQuestProgress turnedIn = questPlayer.Get(identity.NpcKey);
			List<Item> rewardDrops = NewWorldItems(itemsBeforeReward);
			long coinDelta = CoinValue(rewardDrops);
			int cacheType = QuestRewardBalance.CacheItemType(rewardDifficulty);
			int cacheDelta = rewardDrops.Where(item => item.type == cacheType).Sum(item => item.stack);
			int expectedCache = questPlayer.CurrentCompletedTotal() % 5 == 0 ? 2 : 1;
			bool completedNow = !turnedIn.Active && turnedIn.Completed == completedBefore + 1;
			bool rewardNow = coinDelta == rewardCopper && cacheDelta == expectedCache;
			Expect(failures, $"resident-complete:{identity.NpcKey}", completedNow);
			Expect(failures, $"resident-reward:{identity.NpcKey}", rewardNow);
			if (completedNow)
				completed++;
			if (rewardNow)
				rewards++;
			foreach (Item item in rewardDrops)
				item.TurnToAir();
		}
	}

	internal static void PrepareObjective(Player player, ResidentQuestProgress progress, ResidentQuestDefinition definition)
	{
		if (definition.Kind is ResidentQuestKind.DeliverSupplies or ResidentQuestKind.BannerCollection)
		{
			int count = definition.Kind == ResidentQuestKind.BannerCollection ? 1 : definition.Goal;
			GiveItem(player, definition.ItemType, count);
		}
		else
		{
			progress.Progress = definition.Goal;
		}
	}

	private static void GiveItem(Player player, int itemType, int count)
	{
		int remaining = count;
		foreach (Item item in player.inventory)
		{
			if (remaining <= 0)
				break;
			if (!item.IsAir)
				continue;
			item.SetDefaults(itemType);
			item.stack = Math.Min(remaining, Math.Max(1, item.maxStack));
			remaining -= item.stack;
		}

		if (remaining > 0)
			throw new InvalidOperationException($"Synthetic inventory overflow for item {itemType}: {remaining} remaining.");
	}

	private static bool[] CaptureActiveItems() => Main.item.Select(item => item?.active == true).ToArray();
}
