using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootDifficultyNormalizer
{
	internal static bool TryNormalize(IItemDropRule rule, bool preserveBosses)
	{
		if (rule.GetType() == typeof(DropBasedOnExpertMode))
		{
			DropBasedOnExpertMode typed = (DropBasedOnExpertMode)rule;
			Normalize([typed.ruleForNormalMode, typed.ruleForExpertMode],
				wrapped => { typed.ruleForNormalMode = wrapped[0]; typed.ruleForExpertMode = wrapped[1]; },
				preserveBosses, nameof(DropBasedOnExpertMode));
			return true;
		}
		if (rule.GetType() == typeof(DropBasedOnMasterMode))
		{
			DropBasedOnMasterMode typed = (DropBasedOnMasterMode)rule;
			Normalize([typed.ruleForDefault, typed.ruleForMasterMode],
				wrapped => { typed.ruleForDefault = wrapped[0]; typed.ruleForMasterMode = wrapped[1]; },
				preserveBosses, nameof(DropBasedOnMasterMode));
			return true;
		}
		if (rule.GetType() == typeof(DropBasedOnMasterAndExpertMode))
		{
			DropBasedOnMasterAndExpertMode typed = (DropBasedOnMasterAndExpertMode)rule;
			Normalize([typed.ruleForDefault, typed.ruleForExpertmode, typed.ruleForMasterMode],
				wrapped => { typed.ruleForDefault = wrapped[0]; typed.ruleForExpertmode = wrapped[1];
					typed.ruleForMasterMode = wrapped[2]; }, preserveBosses,
				nameof(DropBasedOnMasterAndExpertMode));
			return true;
		}
		return false;
	}

	private static void Normalize(IItemDropRule[] branches, Action<IItemDropRule[]> apply,
		bool preserveBosses, string selector)
	{
		TryBuild(branches, preserveBosses, selector, out IItemDropRule[] wrapped);
		apply(wrapped);
	}

	internal static bool TryBuild(IItemDropRule[] branches, bool preserveBosses,
		string selector, out IItemDropRule[] wrapped)
	{
		wrapped = new IItemDropRule[branches.Length];
		HardLootLeaf[] leaves = new HardLootLeaf[branches.Length];
		HardLootDisposition[] dispositions = new HardLootDisposition[branches.Length];
		bool[] providerAdapted = new bool[branches.Length];
		for (int i = 0; i < branches.Length; i++)
		{
			if (HardLootDifficultyBranchAdapter.TryNormalize(ref branches[i]))
			{
				dispositions[i] = HardLootDisposition.Normalized;
				providerAdapted[i] = true;
				continue;
			}
			dispositions[i] = HardLootClassifier.Inspect(branches[i], out leaves[i]);
			if (dispositions[i] is HardLootDisposition.ReportFailed or HardLootDisposition.Unsupported)
			{
				string items = leaves[i]?.ItemSummary ?? "unknown";
				if (dispositions[i] == HardLootDisposition.ReportFailed)
					HardLootAudit.ReportFailed($"{selector}/branch:{i}", branches[i].GetType().FullName);
				else
					HardLootAudit.Unsupported($"{selector}/branch:{i}",
						branches[i].GetType().FullName, items);
				throw new InvalidOperationException(
					$"Hard-loot cannot normalize {selector} branch {i}: " +
					$"type={branches[i].GetType().FullName}, disposition={dispositions[i]}, items=[{items}].");
			}
		}

		Dictionary<int, float> target = [];
		Dictionary<int, float> baselineRaw = [];
		for (int i = 0; i < branches.Length; i++)
		{
			if (dispositions[i] == HardLootDisposition.Normalized)
			{
				wrapped[i] = branches[i];
				if (!providerAdapted[i]) HardLootAudit.NormalizedPreserved();
				foreach ((int itemId, float rate) in leaves[i]?.Rates ??
					new Dictionary<int, float>())
					if (!target.ContainsKey(itemId))
					{
						target[itemId] = rate;
						baselineRaw[itemId] = rate;
					}
				continue;
			}
			if (dispositions[i] == HardLootDisposition.EmptyBranch)
			{
				wrapped[i] = branches[i];
				HardLootAudit.DropNothingBranchPreserved();
				continue;
			}
			if (dispositions[i] == HardLootDisposition.Guaranteed)
			{
				wrapped[i] = branches[i];
				HardLootAudit.GuaranteedPreserved();
				continue;
			}
			if (dispositions[i] == HardLootDisposition.Protected)
			{
				wrapped[i] = branches[i];
				HardLootAudit.Protected();
				continue;
			}
			int divisor = RequiredDivisor(leaves[i], target);
			wrapped[i] = HardLootRuleFactory.Wrap(branches[i], divisor, preserveBosses);
			if (divisor == 1) HardLootAudit.UltraRare(); else HardLootAudit.Wrapped();
			foreach ((int itemId, float rate) in leaves[i].Rates)
			{
				if (target.TryGetValue(itemId, out float existing))
					HardLootAudit.DifficultyRate(selector, itemId, baselineRaw[itemId], rate, rate / divisor);
				else
				{
					target[itemId] = rate / divisor;
					baselineRaw[itemId] = rate;
				}
			}
		}
		HardLootAudit.Difficulty(branches.Length);
		return true;
	}

	private static int RequiredDivisor(HardLootLeaf leaf, IReadOnlyDictionary<int, float> target)
	{
		double maximum = leaf.ExtraDivisor;
		foreach ((int itemId, float rate) in leaf.Rates)
			if (target.TryGetValue(itemId, out float desired))
				maximum = Math.Max(maximum, rate / desired);
		return Math.Max(1, checked((int)Math.Ceiling(maximum - 0.0000001d)));
	}

}
