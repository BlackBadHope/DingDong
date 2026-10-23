using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootChainSemantics
{
	private const string Failed =
		"Terraria.GameContent.ItemDropRules.Chains+TryIfFailedRandomRoll";
	private const string Succeeded =
		"Terraria.GameContent.ItemDropRules.Chains+TryIfSucceeded";
	private const string Conditions =
		"Terraria.GameContent.ItemDropRules.Chains+TryIfDoesntFillConditions";

	internal static bool HasGuaranteedFailureFallback(IItemDropRule rule, int depth)
	{
		if (!IsPlainCommon(rule) || depth > 16) return false;
		foreach (IItemDropRuleChainAttempt attempt in rule.ChainedRules)
			if (attempt?.GetType().FullName == Failed && attempt.RuleToChain is not null &&
				HardLootRuleSemantics.Inspect(attempt.RuleToChain, depth + 1) ==
				HardLootChanceKind.Guaranteed)
				return true;
		return false;
	}

	internal static bool TrySingleGuaranteedFailureFallback(IItemDropRule rule,
		int depth, out IItemDropRuleChainAttempt fallback)
	{
		fallback = null;
		if (!IsPlainCommon(rule) || rule.ChainedRules.Count != 1 || depth > 16)
			return false;
		IItemDropRuleChainAttempt attempt = rule.ChainedRules[0];
		if (attempt?.GetType() != typeof(Chains.TryIfFailedRandomRoll) ||
			attempt.RuleToChain is not IItemDropRule child || child.ChainedRules.Count != 0 ||
			HardLootRuleSemantics.Inspect(child, depth + 1) != HardLootChanceKind.Guaranteed)
			return false;
		fallback = attempt;
		return true;
	}

	internal static bool CanUniformlyGate(IItemDropRule rule, int depth)
	{
		if (HasOnlyUnreachableConditionBranches(rule)) return true;
		if (HardLootOptionsChainSemantics.CanUniformlyGate(rule, depth, out _))
			return true;
		bool conditional = rule.GetType() == typeof(ItemDropWithConditionRule);
		if ((!IsUniformGateCommon(rule) && !conditional) ||
			rule.ChainedRules.Count == 0 || depth > 16)
			return false;
		foreach (IItemDropRuleChainAttempt attempt in rule.ChainedRules)
		{
			if (!KnownAttempt(attempt) || attempt.RuleToChain is not IItemDropRule child ||
				IsPerPlayer(child) || conditional && attempt.GetType().FullName == Conditions)
				return false;
			if (IsEmpty(child)) continue;
			HardLootChanceKind childKind = HardLootRuleSemantics.Inspect(child, depth + 1);
			if (childKind is HardLootChanceKind.Unknown or HardLootChanceKind.Unsupported)
				return false;
			if (conditional && HardLootRuleSemantics.PreservesGuarantee(childKind) &&
				attempt.GetType().FullName != Succeeded)
				return false;
		}
		return true;
	}

	internal static bool HasOnlyUnreachableConditionBranches(IItemDropRule rule)
	{
		if (!IsPlainCommon(rule) || rule.ChainedRules.Count == 0)
			return false;
		foreach (IItemDropRuleChainAttempt attempt in rule.ChainedRules)
			if (attempt?.GetType().FullName != Conditions || attempt.RuleToChain is null)
				return false;
		return true;
	}

	private static bool KnownAttempt(IItemDropRuleChainAttempt attempt)
	{
		string name = attempt?.GetType().FullName;
		return name == Failed || name == Succeeded || name == Conditions;
	}

	private static bool IsPerPlayer(IItemDropRule rule) =>
		rule.GetType() == typeof(DropPerPlayerOnThePlayer) ||
		rule.GetType() == typeof(DropLocalPerClientAndResetsNPCMoneyTo0);

	private static bool IsEmpty(IItemDropRule rule) => HardLootEmptySemantics.IsEmpty(rule);

	internal static bool IsPlainCommon(IItemDropRule rule)
	{
		System.Type type = rule.GetType();
		return type == typeof(CommonDrop) || type == typeof(CommonDropNotScalingWithLuck);
	}

	private static bool IsUniformGateCommon(IItemDropRule rule) =>
		IsPlainCommon(rule) || rule.GetType() == typeof(CommonDropWithRerolls);
}
