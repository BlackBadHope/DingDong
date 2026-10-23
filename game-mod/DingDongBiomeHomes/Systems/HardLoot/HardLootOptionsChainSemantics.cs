using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootOptionsChainSemantics
{
	internal static bool CanUniformlyGate(IItemDropRule rule, int depth,
		out float rootRate)
	{
		rootRate = 0f;
		if (depth > 16 || rule.ChainedRules.Count == 0 ||
			!TryChance(rule, out int numerator, out int denominator, out int[] ids) ||
			!HardLootRuleSemantics.ValidIds(ids) || numerator <= 0 ||
			denominator <= 0 || numerator >= denominator)
			return false;
		foreach (IItemDropRuleChainAttempt attempt in rule.ChainedRules)
		{
			if (attempt?.GetType() != typeof(Chains.TryIfSucceeded) ||
				attempt.RuleToChain is not IItemDropRule child ||
				child.ChainedRules.Count != 0 || IsPerPlayer(child))
				return false;
			HardLootChanceKind kind = HardLootRuleSemantics.Inspect(child, depth + 1);
			if (kind is HardLootChanceKind.Unknown or HardLootChanceKind.Unsupported)
				return false;
		}
		rootRate = (float)numerator / denominator;
		return rootRate is > 0f and < 1f && !float.IsNaN(rootRate) &&
			!float.IsInfinity(rootRate);
	}

	internal static bool TryUniformRootLeaf(IItemDropRule rule, out HardLootLeaf leaf)
	{
		leaf = null;
		if (!CanUniformlyGate(rule, 0, out float rootRate)) return false;
		List<DropRateInfo> report = [];
		try { rule.ReportDroprates(report, new DropRateInfoChainFeed(1f)); }
		catch { return false; }
		DropRateInfo[] positive = report.Where(drop => drop.dropRate > 0f).ToArray();
		if (positive.Length == 0 || positive.Any(drop => drop.itemId <= 0 ||
			float.IsNaN(drop.dropRate) || float.IsInfinity(drop.dropRate)))
			return false;
		leaf = new HardLootLeaf(rule, positive.Select(drop => drop.itemId).Distinct()
			.ToDictionary(itemId => itemId, _ => rootRate));
		return true;
	}

	private static bool TryChance(IItemDropRule rule, out int numerator,
		out int denominator, out int[] ids)
	{
		if (rule.GetType() == typeof(OneFromOptionsDropRule))
		{
			OneFromOptionsDropRule one = (OneFromOptionsDropRule)rule;
			numerator = one.chanceNumerator;
			denominator = one.chanceDenominator;
			ids = one.dropIds;
			return true;
		}
		if (rule.GetType() == typeof(OneFromOptionsNotScaledWithLuckDropRule))
		{
			OneFromOptionsNotScaledWithLuckDropRule one =
				(OneFromOptionsNotScaledWithLuckDropRule)rule;
			numerator = one.chanceNumerator;
			denominator = one.chanceDenominator;
			ids = one.dropIds;
			return true;
		}
		numerator = denominator = 0;
		ids = null;
		return false;
	}

	private static bool IsPerPlayer(IItemDropRule rule) =>
		rule.GetType() == typeof(DropPerPlayerOnThePlayer) ||
		rule.GetType() == typeof(DropLocalPerClientAndResetsNPCMoneyTo0);
}
