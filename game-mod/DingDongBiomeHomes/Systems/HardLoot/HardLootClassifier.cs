using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal enum HardLootDisposition
{
	Safe,
	EmptyBranch,
	Guaranteed,
	Normalized,
	Protected,
	Unsupported,
	ReportFailed
}

internal sealed record HardLootLeaf(IItemDropRule Rule, IReadOnlyDictionary<int, float> Rates,
	int? CoupledDivisor = null)
{
	internal int ExtraDivisor => CoupledDivisor ??
		HardLootClassifier.ExtraDivisor(Rates.Values.First());
	internal bool HasUniformDivisor => Rates.Values.Select(HardLootClassifier.ExtraDivisor)
		.Distinct().Take(2).Count() == 1;
	internal string ItemSummary => string.Join(",", Rates.Keys.OrderBy(id => id).Take(16));
}

internal static class HardLootClassifier
{
	internal static HardLootDisposition Inspect(IItemDropRule rule, out HardLootLeaf leaf)
	{
		leaf = null;
		if (HardLootEmptySemantics.IsEmpty(rule))
		{
			leaf = new HardLootLeaf(rule, new Dictionary<int, float>());
			return HardLootDisposition.EmptyBranch;
		}
		Type ruleType = rule.GetType();
		bool normalizedRule = ruleType == typeof(HardLootRule) ||
			ruleType == typeof(HardLootNestedRule) || ruleType == typeof(HardLootModeRule) ||
			ruleType == typeof(HardLootPerPlayerRule) ||
			ruleType == typeof(HardLootLocalPerClientRule) ||
			ruleType == typeof(HardLootLeadingConditionRule) ||
			ruleType == typeof(HardLootFailureFallbackRule) ||
			ruleType == typeof(WeaponEnchantmentsCursedEssenceRule);
		HardLootChanceKind semantics = normalizedRule
			? HardLootChanceKind.Unknown : HardLootRuleSemantics.Inspect(rule);
		if (semantics == HardLootChanceKind.Unsupported)
		{
			leaf = RawLeaf(rule);
			return HardLootDisposition.Unsupported;
		}
		if (HardLootRuleSemantics.PreservesGuarantee(semantics))
		{
			leaf = new HardLootLeaf(rule, new Dictionary<int, float>());
			return HardLootDisposition.Guaranteed;
		}
		if (!HardLootReportSemantics.TryReachableRootLeaf(rule, out leaf))
		{
			List<DropRateInfo> report = [];
			try { rule.ReportDroprates(report, new DropRateInfoChainFeed(1f)); }
			catch
			{
				leaf = RawLeaf(rule);
				return HardLootDisposition.ReportFailed;
			}
			if (report.Count == 0) return HardLootDisposition.ReportFailed;
			if (report.Any(drop => drop.dropRate < 0f || float.IsNaN(drop.dropRate) ||
				float.IsInfinity(drop.dropRate) || drop.dropRate > 0f && drop.itemId <= 0))
			{
				leaf = RawLeaf(rule);
				return HardLootDisposition.ReportFailed;
			}
			DropRateInfo[] positive = report.Where(drop => drop.dropRate > 0f).ToArray();
			if (positive.Length == 0)
			{
				leaf = new HardLootLeaf(rule, new Dictionary<int, float>());
				return HardLootDisposition.EmptyBranch;
			}
			leaf = new HardLootLeaf(rule, positive.GroupBy(drop => drop.itemId)
				.ToDictionary(group => group.Key, group => group.Sum(drop => drop.dropRate)));
		}
		IReadOnlyDictionary<int, float> rates = leaf.Rates;
		if (normalizedRule)
			return HardLootDisposition.Normalized;
		if (HardLootCoupledChainSemantics.TryDivisor(rule, semantics, leaf,
			out int coupledDivisor))
			leaf = leaf with { CoupledDivisor = coupledDivisor };
		bool coupled = leaf.CoupledDivisor.HasValue;
		bool hasProtected = rates.Keys.Any(IsProtectedItem);
		bool hasHardenable = rates.Keys.Any(id => !IsProtectedItem(id));
		if (hasProtected)
			return hasHardenable
				? coupled ? HardLootDisposition.Safe : HardLootDisposition.Unsupported
				: HardLootDisposition.Protected;
		if (semantics == HardLootChanceKind.Unknown)
			return HardLootDisposition.Unsupported;
		if (!leaf.HasUniformDivisor && !coupled)
			return HardLootDisposition.Unsupported;
		return HardLootDisposition.Safe;
	}

	private static HardLootLeaf RawLeaf(IItemDropRule rule)
	{
		if (HardLootLeafSemantics.IsExactCommon(rule.GetType()) && rule is CommonDrop common)
			return new HardLootLeaf(rule, new Dictionary<int, float> { [common.itemId] = 0f });
		return null;
	}

	internal static int ExtraDivisor(float rate)
	{
		if (rate >= 1f) return 1;
		if (rate <= 1f / 4000f + 0.0000001f) return 1;
		if (rate <= 0.02f) return 4;
		if (rate <= 0.10f) return 6;
		return 8;
	}

	internal static bool IsProtectedItem(int itemId)
		=> HardLootItemProtection.IsProtected(itemId);
}
