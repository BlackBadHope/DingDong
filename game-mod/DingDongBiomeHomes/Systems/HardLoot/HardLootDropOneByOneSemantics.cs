using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootDropOneByOneSemantics
{
	internal static HardLootChanceKind Inspect(DropOneByOne drop)
	{
		DropOneByOne.Parameters value = drop.parameters;
		if (drop.itemId <= 0 || value.ChanceNumerator <= 0 ||
			value.ChanceDenominator <= 0 ||
			value.ChanceDenominator > int.MaxValue / 2 ||
			value.MinimumItemDropsCount <= 0 ||
			value.MaximumItemDropsCount < value.MinimumItemDropsCount ||
			value.MaximumItemDropsCount == int.MaxValue ||
			value.MinimumStackPerChunkBase <= 0 ||
			value.MaximumStackPerChunkBase < value.MinimumStackPerChunkBase ||
			value.BonusMinDropsPerChunkPerPlayer < 0 ||
			value.BonusMaxDropsPerChunkPerPlayer <
				value.BonusMinDropsPerChunkPerPlayer)
			return HardLootChanceKind.Unsupported;
		long minimumCapacity = value.MinimumStackPerChunkBase +
			(long)Main.maxPlayers * value.BonusMinDropsPerChunkPerPlayer;
		long maximumCapacity = value.MaximumStackPerChunkBase +
			(long)Main.maxPlayers * value.BonusMaxDropsPerChunkPerPlayer;
		long reportMinimum = (long)value.MinimumItemDropsCount *
			(value.MinimumStackPerChunkBase +
				(long)value.BonusMinDropsPerChunkPerPlayer);
		long reportMaximum = (long)value.MaximumItemDropsCount *
			(value.MaximumStackPerChunkBase +
				(long)value.BonusMaxDropsPerChunkPerPlayer);
		if (minimumCapacity <= 0 || maximumCapacity < minimumCapacity ||
			maximumCapacity > int.MaxValue - 1L || reportMinimum <= 0 ||
			reportMaximum < reportMinimum || reportMaximum > int.MaxValue)
			return HardLootChanceKind.Unsupported;
		return HardLootRuleSemantics.Chance(
			value.ChanceNumerator, value.ChanceDenominator);
	}
}
