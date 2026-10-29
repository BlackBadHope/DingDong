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
	// First stage of every resident's personal story, end to end. The lifecycle resident
	// (cycleKey) already finished its whole story in RunProbe: it must refuse a new stage.
	private static void RunAllResidents(Player player, NPC npc, NpcQuestPlayer questPlayer, string cycleKey, List<string> failures, ref int accepted, ref int completed, ref int rewards, ref bool storyCompleteRefused)
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
			if (string.Equals(identity.NpcKey, cycleKey, StringComparison.Ordinal))
			{
				int storyLength = StoryLength(identity.NpcKey);
				(long Coins, int Items) heldBefore = Holdings(player, ItemID.None);
				bool[] itemsBeforeRefusal = CaptureActiveItems();
				InteractWithSyntheticResident(player, npc);
				ResidentQuestProgress after = questPlayer.Get(identity.NpcKey);
				List<Item> refusalDrops = NewWorldItems(itemsBeforeRefusal);
				storyCompleteRefused = !after.Active && after.Completed == storyLength && completedBefore == storyLength && refusalDrops.Count == 0 && Holdings(player, ItemID.None).Coins == heldBefore.Coins;
				Expect(failures, $"story-complete-refusal:{identity.NpcKey}", storyCompleteRefused);
				foreach (Item item in refusalDrops)
					item.TurnToAir();
				continue;
			}

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
			Expect(failures, $"resident-ready:{identity.NpcKey}", NpcQuestSystem.IsReady(player, definition, progress));
			bool rewardNow = TurnInAndVerifyReward(player, npc, questPlayer, definition, rewardCopper, rewardDifficulty, out string rewardDetail);
			ResidentQuestProgress turnedIn = questPlayer.Get(identity.NpcKey);
			bool completedNow = !turnedIn.Active && turnedIn.Completed == completedBefore + 1;
			Expect(failures, $"resident-complete:{identity.NpcKey}", completedNow);
			Expect(failures, $"resident-reward:{identity.NpcKey}({rewardDetail})", rewardNow);
			if (completedNow)
				completed++;
			if (rewardNow)
				rewards++;
		}
	}

	internal static int StoryLength(string npcKey) => PersonalResidentQuestCatalog.Story(npcKey)?.Steps.Length ?? 0;

	// Form-aware objective staging (same rules as the "forms" probe): supplies are placed in
	// the performer's inventory, an Expedition copy carries the stage's origin when the plain
	// copy does not count, a Dialogue Choice records a real pick (Progress 1 of Goal 1), a
	// Delayed stage runs on the world quest clock; the other forms report their goal directly.
	internal static void PrepareObjective(Player player, ResidentQuestProgress progress, ResidentQuestDefinition definition)
	{
		ResidentStoryStep step = definition.StoryKey.Length > 0 ? PersonalResidentQuestCatalog.Step(definition.StoryKey) : null;
		switch (definition.Kind)
		{
			case ResidentQuestKind.DeliverSupplies or ResidentQuestKind.BannerCollection:
				GiveItem(player, SupplyType(definition, step), definition.Kind == ResidentQuestKind.BannerCollection ? 1 : definition.Goal);
				break;
			case ResidentQuestKind.Expedition:
			{
				int supply = SupplyType(definition, step);
				GiveItem(player, supply, definition.Goal);
				if (!NpcQuestSystem.IsReady(player, definition, progress) && step != null && step.SourceTag.Length > 0)
				{
					PropertyInfo tag = typeof(DamageAffinityGlobalItem).GetProperty(nameof(DamageAffinityGlobalItem.AcquisitionTag));
					foreach (Item item in player.inventory.Where(item => item.type == supply && item.stack > 0))
						if (item.TryGetGlobalItem(out DamageAffinityGlobalItem affinity))
							tag?.SetValue(affinity, step.SourceTag);
				}
				break;
			}
			case ResidentQuestKind.DialogueChoice when step != null && step.Choices.Length > 0 && definition.StoryKey.LastIndexOf(':') > 0:
				NpcQuestWorldLedger.RecordChoice(definition.StoryKey[..definition.StoryKey.LastIndexOf(':')], definition.Variant, step.Choices[0].Id);
				progress.Progress = 1;
				break;
			case ResidentQuestKind.Delayed:
				NpcQuestWorldLedger.AdvanceClockForProbe(Math.Max(1, definition.Goal - progress.Progress));
				if (progress.Progress < definition.Goal)
					progress.Progress = definition.Goal;
				break;
			default:
				progress.Progress = definition.Goal;
				break;
		}
	}

	private static int SupplyType(ResidentQuestDefinition definition, ResidentStoryStep step) =>
		step != null && step.Alternatives.Length > 0 && !step.Alternatives.Contains(definition.ItemType) ? step.Alternatives[0] : definition.ItemType;

	// Turns the stage in and checks the reward where production delivers it: personal stories
	// put coins and the reward item into the performer's inventory (overflow drops into the
	// world, counted too); a legacy pool quest pays coins and resident caches.
	private static bool TurnInAndVerifyReward(Player player, NPC npc, NpcQuestPlayer questPlayer, ResidentQuestDefinition definition, int rewardCopper, int rewardDifficulty, out string detail)
	{
		bool personal = definition.StoryKey.Length > 0;
		int rewardType = personal ? definition.RewardItemType : QuestRewardBalance.CacheItemType(rewardDifficulty);
		ResidentStoryStep step = personal ? PersonalResidentQuestCatalog.Step(definition.StoryKey) : null;
		int supply = definition.Kind is ResidentQuestKind.DeliverSupplies or ResidentQuestKind.Expedition or ResidentQuestKind.BannerCollection ? SupplyType(definition, step) : ItemID.None;
		int consumed = definition.Kind == ResidentQuestKind.BannerCollection ? 1 : supply != ItemID.None ? definition.Goal : 0;
		(long Coins, int Items) before = Holdings(player, rewardType);
		bool[] itemsBefore = CaptureActiveItems();
		InteractWithSyntheticResident(player, npc);
		(long Coins, int Items) after = Holdings(player, rewardType);
		List<Item> drops = NewWorldItems(itemsBefore);
		long coinDelta = after.Coins - before.Coins + CoinValue(drops);
		int itemDelta = after.Items - before.Items + drops.Where(item => item.type == rewardType).Sum(item => item.stack);
		foreach (Item item in drops)
			item.TurnToAir();
		long expectedCoins = rewardCopper + (personal && CoinUnit(rewardType) > 0 ? (long)definition.RewardItemCount * CoinUnit(rewardType) : 0L) - (CoinUnit(supply) > 0 ? (long)consumed * CoinUnit(supply) : 0L);
		bool checkItems = CoinUnit(rewardType) == 0;
		int expectedItems = personal ? definition.RewardItemCount - (supply == rewardType ? consumed : 0) : questPlayer.CurrentCompletedTotal() % 5 == 0 ? 2 : 1;
		detail = $"coins={coinDelta}/{expectedCoins};item{rewardType}={(checkItems ? itemDelta + "/" + expectedItems : "coin")}";
		return coinDelta == expectedCoins && (!checkItems || itemDelta == expectedItems);
	}

	private static long CoinUnit(int itemType) => itemType switch
	{
		ItemID.PlatinumCoin => 1_000_000L,
		ItemID.GoldCoin => 10_000L,
		ItemID.SilverCoin => 100L,
		ItemID.CopperCoin => 1L,
		_ => 0L
	};

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
