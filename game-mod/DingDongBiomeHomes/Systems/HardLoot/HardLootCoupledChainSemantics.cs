using System;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootCoupledChainSemantics
{
	internal static bool TryDivisor(IItemDropRule rule, HardLootChanceKind semantics,
		HardLootLeaf leaf, out int divisor)
	{
		divisor = 0;
		if (semantics != HardLootChanceKind.Optional || !IsCoupled(rule) ||
			leaf?.Rates is null || leaf.Rates.Count == 0 ||
			(rule.ChainedRules.Count != 0 &&
				!HardLootChainSemantics.CanUniformlyGate(rule, 0)))
			return false;

		foreach (float rate in leaf.Rates.Values)
		{
			int candidate = HardLootClassifier.ExtraDivisor(rate);
			if (candidate == 1) return false;
			divisor = Math.Max(divisor, candidate);
		}
		return divisor > 1;
	}

	private static bool IsCoupled(IItemDropRule rule)
	{
		if (rule.ChainedRules.Count != 0) return true;
		Type type = rule.GetType();
		return type == typeof(OneFromRulesRule) || type == typeof(FewFromRulesRule) ||
			type == typeof(SequentialRulesRule);
	}
}
