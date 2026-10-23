using System;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootCompositeSemantics
{
	internal static HardLootChanceKind Inspect(IItemDropRule rule, int depth)
	{
		Type type = rule.GetType();
		if (type == typeof(OneFromRulesRule))
		{
			OneFromRulesRule one = (OneFromRulesRule)rule;
			return Rules(one.options, one.chanceNumerator, one.chanceDenominator,
				depth, requireAll: true);
		}
		if (type == typeof(FewFromRulesRule))
		{
			FewFromRulesRule few = (FewFromRulesRule)rule;
			if (few.options is null || few.amount <= 0 || few.amount > few.options.Length)
				return HardLootChanceKind.Unsupported;
			return FewRules(few.options, few.amount, few.chanceDenominator, depth);
		}
		if (type == typeof(SequentialRulesRule))
		{
			SequentialRulesRule sequence = (SequentialRulesRule)rule;
			return Rules(sequence.rules, 1, sequence.chanceDenominator,
				depth, requireAll: false);
		}
		if (type == typeof(AlwaysAtleastOneSuccessDropRule))
		{
			AlwaysAtleastOneSuccessDropRule always = (AlwaysAtleastOneSuccessDropRule)rule;
			HardLootChanceKind[] children = Children(always.rules, depth);
			if (children.Length == 0 || children.Any(UnknownOrUnsupported))
				return HardLootChanceKind.Unsupported;
			if (children.Any(kind => kind == HardLootChanceKind.ConditionalGuaranteed))
				return HardLootChanceKind.Unsupported;
			return children.Any(kind => kind == HardLootChanceKind.Guaranteed)
				? HardLootChanceKind.Guaranteed : HardLootChanceKind.Unsupported;
		}
		return HardLootChanceKind.Unknown;
	}

	private static HardLootChanceKind Rules(IItemDropRule[] rules, int numerator,
		int denominator, int depth, bool requireAll)
	{
		if (rules is not { Length: > 0 }) return HardLootChanceKind.Unsupported;
		HardLootChanceKind chance = HardLootRuleSemantics.Chance(numerator, denominator);
		if (chance == HardLootChanceKind.Unsupported) return chance;
		HardLootChanceKind[] children = Children(rules, depth);
		if (children.Any(UnknownOrUnsupported)) return HardLootChanceKind.Unsupported;
		if (chance != HardLootChanceKind.Guaranteed) return HardLootChanceKind.Optional;
		if (children.Any(kind => kind == HardLootChanceKind.ConditionalGuaranteed))
			return HardLootChanceKind.Unsupported;
		bool proven = requireAll
			? children.All(kind => kind == HardLootChanceKind.Guaranteed)
			: children.Any(kind => kind == HardLootChanceKind.Guaranteed);
		return proven ? HardLootChanceKind.Guaranteed : HardLootChanceKind.Optional;
	}

	private static HardLootChanceKind FewRules(IItemDropRule[] rules, int amount,
		int denominator, int depth)
	{
		HardLootChanceKind chance = HardLootRuleSemantics.Chance(1, denominator);
		if (chance == HardLootChanceKind.Unsupported) return chance;
		HardLootChanceKind[] children = Children(rules, depth);
		if (children.Any(UnknownOrUnsupported)) return HardLootChanceKind.Unsupported;
		if (chance != HardLootChanceKind.Guaranteed) return HardLootChanceKind.Optional;
		if (children.Any(kind => kind == HardLootChanceKind.ConditionalGuaranteed))
			return HardLootChanceKind.Unsupported;
		int nonGuaranteed = children.Count(kind => kind != HardLootChanceKind.Guaranteed);
		return amount > nonGuaranteed
			? HardLootChanceKind.Guaranteed : HardLootChanceKind.Optional;
	}

	private static HardLootChanceKind[] Children(IItemDropRule[] rules, int depth) =>
		rules is null ? [] : rules.Select(rule => rule?.GetType() == typeof(DropNothing)
			? rule.ChainedRules.Count == 0
				? HardLootChanceKind.Optional : HardLootChanceKind.Unsupported
			: HardLootRuleSemantics.Inspect(rule, depth + 1)).ToArray();

	private static bool UnknownOrUnsupported(HardLootChanceKind kind) =>
		kind is HardLootChanceKind.Unknown or HardLootChanceKind.Unsupported;
}
