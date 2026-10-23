using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootLeadingConditionNormalizer
{
	internal static bool TryNormalize(IItemDropRule rule, string scope, int depth,
		out IItemDropRule replacement)
	{
		replacement = null;
		if (rule.GetType() == typeof(HardLootLeadingConditionRule))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		if (rule.GetType() != typeof(LeadingConditionRule)) return false;
		if (depth > 16) throw Refused(scope, "nesting depth");
		LeadingConditionRule leading = (LeadingConditionRule)rule;
		if (leading.condition is null) throw Refused(scope, "null condition");
		if (leading.ChainedRules.Count == 0)
		{
			HardLootAudit.DropNothingBranchPreserved();
			return true;
		}
		List<IItemDropRuleChainAttempt> chains = [];
		for (int i = 0; i < leading.ChainedRules.Count; i++)
		{
			IItemDropRuleChainAttempt attempt = leading.ChainedRules[i];
			if (attempt?.RuleToChain is not IItemDropRule child)
				throw Refused(scope, $"chain {i} target");
			if (attempt.GetType() == typeof(Chains.TryIfFailedRandomRoll))
			{
				chains.Add(Rebuild(attempt, child, scope, i));
				continue;
			}
			if (attempt.GetType() != typeof(Chains.TryIfSucceeded) &&
				attempt.GetType() != typeof(Chains.TryIfDoesntFillConditions))
				throw Refused(scope, $"chain {i} type {attempt.GetType().FullName}");
			IItemDropRule normalized = HardLootDetachedRuleNormalizer.Normalize(
				child, $"{scope}/chain:{i}", depth + 1);
			chains.Add(Rebuild(attempt, normalized, scope, i));
		}
		replacement = new HardLootLeadingConditionRule(leading.condition, chains);
		HardLootAudit.Wrapped();
		return true;
	}

	private static IItemDropRuleChainAttempt Rebuild(IItemDropRuleChainAttempt attempt,
		IItemDropRule child, string scope, int index)
	{
		if (attempt.GetType() == typeof(Chains.TryIfSucceeded))
			return new Chains.TryIfSucceeded(child,
				((Chains.TryIfSucceeded)attempt).hideLootReport);
		if (attempt.GetType() == typeof(Chains.TryIfFailedRandomRoll))
			return new Chains.TryIfFailedRandomRoll(child,
				((Chains.TryIfFailedRandomRoll)attempt).hideLootReport);
		if (attempt.GetType() == typeof(Chains.TryIfDoesntFillConditions))
			return new Chains.TryIfDoesntFillConditions(child,
				((Chains.TryIfDoesntFillConditions)attempt).hideLootReport);
		throw Refused(scope, $"chain {index} type {attempt.GetType().FullName}");
	}

	private static InvalidOperationException Refused(string scope, string detail) =>
		new($"Hard-loot refused LeadingConditionRule {scope}: {detail}.");
}
