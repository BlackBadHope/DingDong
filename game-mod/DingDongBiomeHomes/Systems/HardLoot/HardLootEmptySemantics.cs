using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootEmptySemantics
{
	internal static bool IsEmpty(IItemDropRule rule)
	{
		if (rule.GetType() == typeof(DropNothing) && rule.ChainedRules.Count == 0)
			return true;
		if (rule.GetType() == typeof(DropOneByOne) && rule.ChainedRules.Count == 0)
		{
			DropOneByOne.Parameters value = ((DropOneByOne)rule).parameters;
			return value.ChanceNumerator == 0 && value.ChanceDenominator > 0 &&
				value.ChanceDenominator <= int.MaxValue / 2;
		}
		if (rule.ChainedRules.Count != 0 || rule is not CommonDrop common)
			return false;
		if (HardLootLeafSemantics.IsExactCommon(rule.GetType()) &&
			common.chanceNumerator == 0 && common.chanceDenominator > 0)
			return true;
		return rule.GetType() == typeof(CommonDrop) && common.itemId == 0 &&
			common.chanceNumerator >= 0 && common.chanceDenominator > 0 &&
			common.amountDroppedMinimum > 0 &&
			common.amountDroppedMaximum >= common.amountDroppedMinimum;
	}
}
