using System;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal enum HardLootChanceKind
{
	Unknown,
	Optional,
	Guaranteed,
	ConditionalGuaranteed,
	Unsupported
}

internal static class HardLootRuleSemantics
{
	internal static HardLootChanceKind Inspect(IItemDropRule rule, int depth = 0)
	{
		if (rule is null || depth > 16) return HardLootChanceKind.Unsupported;
		if (HardLootGuaranteedFallbackTree.IsGuaranteed(rule, depth))
			return HardLootChanceKind.Guaranteed;
		HardLootChanceKind leaf = HardLootLeafSemantics.Inspect(rule);
		HardLootChanceKind kind = leaf != HardLootChanceKind.Unknown
			? leaf : HardLootCompositeSemantics.Inspect(rule, depth);
		if (rule.ChainedRules.Count != 0 && !PreservesGuarantee(kind))
		{
			if (kind == HardLootChanceKind.Optional &&
				HardLootChainSemantics.HasGuaranteedFailureFallback(rule, depth))
				return HardLootChanceKind.Unsupported;
			if (kind == HardLootChanceKind.Optional &&
				HardLootChainSemantics.CanUniformlyGate(rule, depth))
				return HardLootChanceKind.Optional;
			return HardLootChanceKind.Unsupported;
		}
		return kind;
	}

	internal static bool PreservesGuarantee(HardLootChanceKind kind) =>
		kind is HardLootChanceKind.Guaranteed or HardLootChanceKind.ConditionalGuaranteed;

	internal static HardLootChanceKind Chance(int numerator, int denominator,
		bool conditional = false)
	{
		if (numerator <= 0 || denominator <= 0) return HardLootChanceKind.Unsupported;
		if (numerator < denominator) return HardLootChanceKind.Optional;
		return conditional
			? HardLootChanceKind.ConditionalGuaranteed : HardLootChanceKind.Guaranteed;
	}

	internal static bool ValidIds(int[] ids) =>
		ids is { Length: > 0 } && Array.TrueForAll(ids, id => id > 0);
}
