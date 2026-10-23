using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal readonly record struct QuestDifficultySnapshot(int Rank, float GoalMultiplier, float RewardMultiplier, string Key);
internal readonly record struct QuestRewardReceipt(int CopperValue, int CacheItemType, int CacheCount, bool MilestoneBonus, int ThemedItemType, int ThemedItemCount);

internal static class QuestRewardBalance
{
	internal static QuestDifficultySnapshot Capture()
	{
		if (Main.masterMode && Main.getGoodWorld)
			return new QuestDifficultySnapshot(3, 1.8f, 2.75f, "Legendary");
		if (Main.masterMode)
			return new QuestDifficultySnapshot(2, 1.5f, 2.0f, "Master");
		if (Main.expertMode)
			return new QuestDifficultySnapshot(1, 1.25f, 1.5f, "Expert");
		return new QuestDifficultySnapshot(0, 1f, 1f, "Classic");
	}

	internal static ResidentQuestDefinition Scale(ResidentQuestDefinition definition, out int rewardCopper, out int difficultyRank)
	{
		QuestDifficultySnapshot difficulty = Capture();
		difficultyRank = difficulty.Rank;
		bool fixedGoal = definition.Kind is ResidentQuestKind.SurveyOrigin
			or ResidentQuestKind.BossHunt
			or ResidentQuestKind.BannerCollection
			or ResidentQuestKind.ModProgression;
		int scaledGoal = fixedGoal
			? definition.Goal
			: Math.Max(1, (int)Math.Ceiling(definition.Goal * difficulty.GoalMultiplier));
		float progressionMultiplier = 1f
			+ Math.Min(0.8f, QuestWorldProgressSystem.DefeatedBossCount * 0.04f)
			+ (Main.hardMode ? 0.25f : 0f);
		int baseReward = definition.BaseRewardCopper > 0 ? definition.BaseRewardCopper : 2500;
		rewardCopper = Math.Max(100, (int)Math.Round(baseReward * difficulty.RewardMultiplier * progressionMultiplier / 100f) * 100);
		return definition with { Goal = scaledGoal };
	}

	internal static QuestRewardReceipt Grant(Player player, NpcTownIdentity identity, int copperValue, int difficultyRank, int totalCompleted)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || copperValue <= 0)
			return default;
		IEntitySource source = player.GetSource_Misc("DingDongResidentQuestReward");
		int remaining = copperValue;
		Spawn(ItemID.PlatinumCoin, 1_000_000);
		Spawn(ItemID.GoldCoin, 10_000);
		Spawn(ItemID.SilverCoin, 100);
		Spawn(ItemID.CopperCoin, 1);
		int cacheItemType = CacheItemType(difficultyRank);
		bool milestoneBonus = totalCompleted > 0 && totalCompleted % 5 == 0;
		int cacheCount = milestoneBonus ? 2 : 1;
		player.QuickSpawnItem(source, cacheItemType, cacheCount);
		ThematicRewardRoll themed = QuestThematicReward.Roll(identity, difficultyRank);
		player.QuickSpawnItem(source, themed.ItemType, themed.Stack);
		return new QuestRewardReceipt(copperValue, cacheItemType, cacheCount, milestoneBonus, themed.ItemType, themed.Stack);

		void Spawn(int itemType, int unitValue)
		{
			int count = remaining / unitValue;
			if (count <= 0)
				return;
			remaining -= count * unitValue;
			player.QuickSpawnItem(source, itemType, count);
		}
	}

	internal static int CacheItemType(int difficultyRank) => Math.Clamp(difficultyRank, 0, 3) switch
	{
		0 => ModContent.ItemType<ClassicResidentCache>(),
		1 => ModContent.ItemType<ExpertResidentCache>(),
		2 => ModContent.ItemType<MasterResidentCache>(),
		_ => ModContent.ItemType<LegendaryResidentCache>()
	};

	internal static string FormatBundle(int copperValue, int difficultyRank) =>
		$"{FormatCoins(copperValue)} + {Lang.GetItemNameValue(CacheItemType(difficultyRank))} + {Language.GetTextValue("Mods.DingDongBiomeHomes.Quests.ThemedBonus")}";

	internal static string FormatCoins(int copperValue)
	{
		int platinum = copperValue / 1_000_000;
		int gold = copperValue / 10_000 % 100;
		int silver = copperValue / 100 % 100;
		int copper = copperValue % 100;
		return $"{platinum}p {gold}g {silver}s {copper}c";
	}
}
