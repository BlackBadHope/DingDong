using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootRuleDiagnostic
{
	internal static string Describe(IItemDropRule rule)
	{
		try
		{
			List<string> parts = [$"chains={rule.ChainedRules.Count}"];
			if (rule.GetType() == typeof(OneFromOptionsDropRule))
			{
				OneFromOptionsDropRule one = (OneFromOptionsDropRule)rule;
				parts.Add($"chance={one.chanceNumerator}/{one.chanceDenominator}");
				parts.Add($"ids={Ids(one.dropIds)}");
			}
			else if (rule.GetType() == typeof(OneFromOptionsNotScaledWithLuckDropRule))
			{
				OneFromOptionsNotScaledWithLuckDropRule one =
					(OneFromOptionsNotScaledWithLuckDropRule)rule;
				parts.Add($"chance={one.chanceNumerator}/{one.chanceDenominator}");
				parts.Add($"ids={Ids(one.dropIds)}");
			}
			for (int i = 0; i < rule.ChainedRules.Count; i++)
				parts.Add(Chain(i, rule.ChainedRules[i]));
			return string.Join(";", parts);
		}
		catch (Exception exception)
		{
			return $"diagnostic-failed={exception.GetType().Name}";
		}
	}

	private static string Chain(int index, IItemDropRuleChainAttempt attempt)
	{
		string attemptType = attempt?.GetType().FullName ?? "null";
		if (attempt?.RuleToChain is not IItemDropRule child)
			return $"chain{index}={attemptType}->null";
		string childType = child.GetType().FullName ?? child.GetType().Name;
		HardLootChanceKind kind = HardLootRuleSemantics.Inspect(child, 1);
		string detail = child is CommonDrop common
			? $",item={common.itemId},chance={common.chanceNumerator}/{common.chanceDenominator}"
			: string.Empty;
		return $"chain{index}={attemptType}->{childType},kind={kind}," +
			$"chains={child.ChainedRules.Count}{detail}";
	}

	private static string Ids(int[] ids) => ids is null ? "null" :
		string.Join(",", ids.Take(16));
}
