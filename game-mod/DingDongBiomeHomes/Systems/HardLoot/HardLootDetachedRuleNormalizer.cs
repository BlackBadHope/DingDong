using System;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootDetachedRuleNormalizer
{
	internal static IItemDropRule Normalize(IItemDropRule rule, string scope)
		=> Normalize(rule, scope, 0);

	internal static IItemDropRule Normalize(IItemDropRule rule, string scope, int depth)
	{
		if (depth > 16) throw new InvalidOperationException(
			$"Hard-loot refused {scope}: nested rule depth exceeded.");
		HardLootAudit.Visited();
		if (HardLootProviderRuleAdapters.TryNormalize(rule, out IItemDropRule providerRule))
			return providerRule;
		if (HardLootFailureFallbackNormalizer.TryNormalize(
			rule, depth, out IItemDropRule fallbackRule))
			return fallbackRule;
		if (HardLootLeadingConditionNormalizer.TryNormalize(
			rule, scope, depth, out IItemDropRule leadingRule))
			return leadingRule ?? rule;
		if (FargoSoulsDifficultyAdapter.TryNormalize(rule, false, out IItemDropRule replacement))
			return replacement ?? rule;
		if (HardLootDifficultyNormalizer.TryNormalize(rule, false)) return rule;
		HardLootDisposition result = HardLootClassifier.Inspect(rule, out HardLootLeaf leaf);
		switch (result)
		{
			case HardLootDisposition.EmptyBranch:
				HardLootAudit.DropNothingBranchPreserved();
				return rule;
			case HardLootDisposition.Guaranteed:
				HardLootAudit.GuaranteedPreserved();
				return rule;
			case HardLootDisposition.Normalized:
				HardLootAudit.NormalizedPreserved();
				return rule;
			case HardLootDisposition.Protected:
				HardLootAudit.Protected();
				return rule;
			case HardLootDisposition.ReportFailed:
				HardLootAudit.ReportFailed(scope, rule.GetType().FullName);
				throw Refused(scope, rule, result, "unknown");
			case HardLootDisposition.Unsupported:
				string items = leaf?.ItemSummary ?? "unknown";
				HardLootAudit.Unsupported(scope, rule.GetType().FullName, items);
				throw Refused(scope, rule, result, items);
	}
		if (leaf.ExtraDivisor == 1)
		{
			HardLootAudit.UltraRare();
			return rule;
		}
		HardLootAudit.Wrapped();
		return HardLootRuleFactory.Wrap(rule, leaf.ExtraDivisor, false);
	}

	private static InvalidOperationException Refused(string scope, IItemDropRule rule,
		HardLootDisposition result, string items) => new(
		$"Hard-loot cannot normalize {scope}: type={rule.GetType().FullName}, " +
		$"disposition={result}, items=[{items}], " +
		$"detail=[{HardLootRuleDiagnostic.Describe(rule)}].");
}
