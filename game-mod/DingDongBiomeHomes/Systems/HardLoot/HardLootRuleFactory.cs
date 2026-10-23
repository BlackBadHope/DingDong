using System;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootRuleFactory
{
	internal static IItemDropRule Wrap(IItemDropRule rule, int divisor, bool preserveBosses)
	{
		IItemDropRule perPlayer = HardLootPerPlayerRuleFactory.Rewrite(rule, divisor);
		if (perPlayer is not null)
		{
			if (preserveBosses)
				throw new InvalidOperationException("Per-player boss bypass requires a dedicated proof.");
			return perPlayer;
		}
		return rule is INestedItemDropRule || rule.ChainedRules.Count != 0
			? new HardLootNestedRule(rule, divisor, preserveBosses)
			: new HardLootRule(rule, divisor, preserveBosses);
	}
}
