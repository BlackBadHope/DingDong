using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootRerollSemantics
{
	private const int MaximumAuditableRolls = 1024;

	internal static HardLootChanceKind Inspect(CommonDropWithRerolls drop)
	{
		if (drop.itemId <= 0 || drop.amountDroppedMinimum <= 0 ||
			drop.amountDroppedMaximum < drop.amountDroppedMinimum ||
			drop.timesToRoll <= 0 || drop.timesToRoll > MaximumAuditableRolls)
			return HardLootChanceKind.Unsupported;
		HardLootChanceKind chance = HardLootRuleSemantics.Chance(
			drop.chanceNumerator, drop.chanceDenominator);
		if (chance != HardLootChanceKind.Optional) return chance;
		float rate = EffectiveRate(drop);
		return rate > 0f && rate < 1f && !float.IsNaN(rate) &&
			!float.IsInfinity(rate) ? chance : HardLootChanceKind.Unsupported;
	}

	internal static bool TryUniformRootRate(IItemDropRule rule, out float rate)
	{
		rate = 0f;
		if (rule.GetType() != typeof(CommonDropWithRerolls) ||
			rule.ChainedRules.Count == 0 ||
			!HardLootChainSemantics.CanUniformlyGate(rule, 0) ||
			!HasOnlyGuaranteedSuccessPayload(rule))
			return false;
		CommonDropWithRerolls drop = (CommonDropWithRerolls)rule;
		if (Inspect(drop) != HardLootChanceKind.Optional) return false;
		rate = EffectiveRate(drop);
		return true;
	}

	private static bool HasOnlyGuaranteedSuccessPayload(IItemDropRule rule)
	{
		foreach (IItemDropRuleChainAttempt attempt in rule.ChainedRules)
		{
			if (attempt?.GetType() != typeof(Chains.TryIfSucceeded) ||
				attempt.RuleToChain is not IItemDropRule child ||
				child.ChainedRules.Count != 0 ||
				HardLootRuleSemantics.Inspect(child) != HardLootChanceKind.Guaranteed)
				return false;
		}
		return true;
	}

	private static float EffectiveRate(CommonDropWithRerolls drop)
	{
		float miss = 1f - (float)drop.chanceNumerator / drop.chanceDenominator;
		float allMiss = 1f;
		for (int i = 0; i < drop.timesToRoll; i++) allMiss *= miss;
		return 1f - allMiss;
	}
}
