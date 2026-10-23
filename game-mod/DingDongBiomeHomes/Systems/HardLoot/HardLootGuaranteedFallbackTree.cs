using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootGuaranteedFallbackTree
{
	internal static bool IsGuaranteed(IItemDropRule rule, int depth)
	{
		if (rule is null || depth > 16 || rule.ChainedRules.Count != 1 ||
			BaseChance(rule) != HardLootChanceKind.Optional)
			return false;
		IItemDropRuleChainAttempt attempt = rule.ChainedRules[0];
		if (attempt?.GetType() != typeof(Chains.TryIfFailedRandomRoll) ||
			attempt.RuleToChain is not IItemDropRule child)
			return false;
		HardLootChanceKind childBase = BaseChance(child);
		return child.ChainedRules.Count == 0
			? HardLootRuleSemantics.PreservesGuarantee(childBase)
			: IsGuaranteed(child, depth + 1);
	}

	private static HardLootChanceKind BaseChance(IItemDropRule rule)
	{
		HardLootChanceKind leaf = HardLootLeafSemantics.Inspect(rule);
		return leaf != HardLootChanceKind.Unknown
			? leaf : HardLootCompositeSemantics.Inspect(rule, 0);
	}
}
