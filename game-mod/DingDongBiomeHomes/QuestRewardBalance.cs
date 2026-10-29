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
		if (definition.StoryKey.Length > 0)
		{ rewardCopper = definition.BaseRewardCopper; difficultyRank = Capture().Rank; return definition; }
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

	internal static QuestRewardReceipt GrantPersonal(Player player, ResidentQuestDefinition definition, int copperValue)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || definition.RewardItemType <= 0 || copperValue <= 0) return default;
        var source = player.GetSource_Misc("DingDongResidentStoryReward");
        int remaining = copperValue;
        foreach (var coin in new[] { (ItemID.PlatinumCoin, 1000000), (ItemID.GoldCoin, 10000), (ItemID.SilverCoin, 100), (ItemID.CopperCoin, 1) })
        { int stack = remaining / coin.Item2; remaining %= coin.Item2; if (stack > 0) GivePersonal(player, source, coin.Item1, stack); }
        GivePersonal(player, source, definition.RewardItemType, definition.RewardItemCount);
        return new(copperValue, 0, 0, false, definition.RewardItemType, definition.RewardItemCount);
    }
    private static void GivePersonal(Player player,IEntitySource source,int type,int stack)
    {
        var before=new (int Type,int Stack,int Prefix)[player.inventory.Length];
        for(int i=0;i<before.Length;i++)before[i]=(player.inventory[i].type,player.inventory[i].stack,player.inventory[i].prefix);
        Item remainder=player.GetItem(player.whoAmI,new Item(type,stack),GetItemSettings.NPCEntityToPlayerInventorySettings);
        if(Main.netMode==NetmodeID.Server)
            for(int i=0;i<before.Length;i++)
                if(before[i]!=(player.inventory[i].type,player.inventory[i].stack,(int)player.inventory[i].prefix))
                    NetMessage.SendData(MessageID.SyncEquipment,-1,-1,null,player.whoAmI,i,player.inventory[i].prefix);
        if(remainder.IsAir)return;
        int index=Item.NewItem(source,player.Hitbox,remainder,noBroadcast:true);
        if(Main.netMode==NetmodeID.Server)
        {
            // Same native instanced-item delivery used by per-player boss loot.
            Main.timeItemSlotCannotBeReusedFor[index]=54000;
            NetMessage.SendData(MessageID.InstancedItem,player.whoAmI,-1,null,index);
            Main.item[index].active=false;
        }
    }
    internal static string FormatBundle(ResidentQuestDefinition definition, int copperValue, int difficultyRank) => definition.StoryKey.Length == 0
        ? FormatBundle(copperValue, difficultyRank)
        : $"{FormatCoins(copperValue)} + {definition.RewardItemCount} × {Lang.GetItemNameValue(definition.RewardItemType)}";

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
		bool ru=Language.ActiveCulture.CultureInfo.TwoLetterISOLanguageName=="ru";
        var parts=new System.Collections.Generic.List<string>();
        if(platinum>0)parts.Add(platinum+(ru?" платины":" platinum"));if(gold>0)parts.Add(gold+(ru?" золота":" gold"));
        if(silver>0)parts.Add(silver+(ru?" серебра":" silver"));if(copper>0||parts.Count==0)parts.Add(copper+(ru?" меди":" copper"));
        return string.Join(" ",parts);
	}
}
