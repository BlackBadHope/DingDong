using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootItemProtection
{
	internal static bool IsProtected(int itemId)
	{
		if (!ContentSamples.ItemsByType.TryGetValue(itemId, out Item item) || item.IsAir)
			return true;
		if (item.questItem || (item.material && !IsGear(item)) || IsCoinOrPickup(itemId))
			return true;
		return InSet(ItemID.Sets.BossBag, itemId)
			|| InSet(ItemID.Sets.PreHardmodeLikeBossBag, itemId)
			|| InSet(ItemID.Sets.OpenableBag, itemId)
			|| InSet(ItemID.Sets.IsFishingCrate, itemId)
			|| InSet(ItemID.Sets.IsFishingCrateHardmode, itemId);
	}

	private static bool IsGear(Item item) => item.accessory || item.damage > 0
		|| item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0;

	private static bool IsCoinOrPickup(int itemId) =>
		itemId is >= ItemID.CopperCoin and <= ItemID.PlatinumCoin
		|| itemId is ItemID.Heart or ItemID.CandyApple or ItemID.CandyCane
			or ItemID.Star or ItemID.SoulCake or ItemID.SugarPlum;

	private static bool InSet(bool[] set, int itemId) =>
		itemId >= 0 && itemId < set.Length && set[itemId];
}
