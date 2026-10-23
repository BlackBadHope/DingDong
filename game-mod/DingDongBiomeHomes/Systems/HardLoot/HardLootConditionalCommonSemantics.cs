using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootConditionalCommonSemantics
{
	internal static HardLootChanceKind Inspect(ItemDropWithConditionRule drop)
	{
		if (drop.condition is null || drop.itemId <= 0 || drop.chanceNumerator <= 0 ||
			drop.chanceDenominator <= 0 || drop.chanceDenominator > int.MaxValue / 2 ||
			drop.amountDroppedMinimum <= 0 ||
			drop.amountDroppedMaximum < drop.amountDroppedMinimum ||
			drop.amountDroppedMaximum == int.MaxValue)
			return HardLootChanceKind.Unsupported;
		return HardLootRuleSemantics.Chance(
			drop.chanceNumerator, drop.chanceDenominator, conditional: true);
	}
}
