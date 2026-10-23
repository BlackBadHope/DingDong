using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootReportSemantics
{
	internal static bool TryReachableRootLeaf(IItemDropRule rule, out HardLootLeaf leaf)
	{
		leaf = null;
		if (HardLootRerollSemantics.TryUniformRootRate(rule, out float rerollRate))
		{
			CommonDrop reroll = (CommonDrop)rule;
			leaf = new HardLootLeaf(rule,
				new Dictionary<int, float> { [reroll.itemId] = rerollRate });
			return true;
		}
		if (HardLootOptionsChainSemantics.TryUniformRootLeaf(rule, out leaf))
			return true;
		if (!HardLootChainSemantics.HasOnlyUnreachableConditionBranches(rule))
			return false;
		CommonDrop common = (CommonDrop)rule;
		float rate = (float)common.chanceNumerator / common.chanceDenominator;
		leaf = new HardLootLeaf(rule,
			new Dictionary<int, float> { [common.itemId] = rate });
		return true;
	}

	internal static void ReportGameplayRates(IItemDropRule rule,
		List<DropRateInfo> drops, DropRateInfoChainFeed feed)
	{
		if (!HardLootChainSemantics.HasOnlyUnreachableConditionBranches(rule))
		{
			rule.ReportDroprates(drops, feed);
			return;
		}
		CommonDrop common = (CommonDrop)rule;
		float rate = (float)common.chanceNumerator / common.chanceDenominator;
		drops.Add(new DropRateInfo(common.itemId, common.amountDroppedMinimum,
			common.amountDroppedMaximum, rate * feed.parentDroprateChance,
			feed.conditions));
	}
}
