using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public abstract class ResidentQuestCacheItem : ModItem
{
	protected abstract int TextureItemType { get; }
	protected abstract int Rarity { get; }
	protected abstract int DifficultyRank { get; }

	public override string Texture => $"Terraria/Images/Item_{TextureItemType}";

	public override void SetDefaults()
	{
		Item.width = 28;
		Item.height = 28;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.rare = Rarity;
		Item.value = 0;
	}

	public override bool CanRightClick() => true;

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		ThematicRewardOption[] pool = QuestThematicReward.CachePool(DifficultyRank);
		int rolls = 3 + DifficultyRank;
		for (int roll = 0; roll < rolls; roll++)
		{
			IItemDropRule[] options = pool
				.Select(option => ItemDropRule.Common(option.ItemType, 1, option.MinStack, option.MaxStack))
				.ToArray();
			itemLoot.Add(new OneFromRulesRule(1, options));
		}
	}
}

public sealed class ClassicResidentCache : ResidentQuestCacheItem
{
	protected override int TextureItemType => ItemID.WoodenCrate;
	protected override int Rarity => ItemRarityID.Blue;
	protected override int DifficultyRank => 0;
}

public sealed class ExpertResidentCache : ResidentQuestCacheItem
{
	protected override int TextureItemType => ItemID.IronCrate;
	protected override int Rarity => ItemRarityID.Green;
	protected override int DifficultyRank => 1;
}

public sealed class MasterResidentCache : ResidentQuestCacheItem
{
	protected override int TextureItemType => ItemID.GoldenCrate;
	protected override int Rarity => ItemRarityID.Orange;
	protected override int DifficultyRank => 2;
}

public sealed class LegendaryResidentCache : ResidentQuestCacheItem
{
	protected override int TextureItemType => ItemID.ObsidianLockbox;
	protected override int Rarity => ItemRarityID.LightRed;
	protected override int DifficultyRank => 3;
}
