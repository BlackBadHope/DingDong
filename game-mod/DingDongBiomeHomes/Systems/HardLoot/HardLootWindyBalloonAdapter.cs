using System;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootWindyBalloonAdapter
{
	private static readonly int[] ExpectedItems =
		[4367, 4368, 4369, 4370, 4371, 4612, 4674, 4343, 4344];

	internal static bool TryNormalize(IItemDropRule rule, out IItemDropRule normalized)
	{
		normalized = rule;
		if (rule.GetType() != typeof(OneFromOptionsDropRule)) return false;
		OneFromOptionsDropRule gate = (OneFromOptionsDropRule)rule;
		if (gate.dropIds is not { Length: 0 }) return false;
		Validate(gate);
		IItemDropRule repaired = new HardLootWindyBalloonRule(gate);
		normalized = HardLootRuleFactory.Wrap(repaired,
			HardLootClassifier.ExtraDivisor(1f / 8f), false);
		HardLootAudit.Wrapped();
		return true;
	}

	private static void Validate(OneFromOptionsDropRule gate)
	{
		if (gate.chanceNumerator != 1 || gate.chanceDenominator != 8 ||
			gate.ChainedRules.Count != ExpectedItems.Length)
			throw Changed("root chance or chain count");
		for (int i = 0; i < ExpectedItems.Length; i++)
		{
			IItemDropRuleChainAttempt attempt = gate.ChainedRules[i];
			if (attempt?.GetType() != typeof(Chains.TryIfSucceeded) ||
				attempt.RuleToChain?.GetType() != typeof(CommonDrop))
				throw Changed($"chain {i} type");
			CommonDrop child = (CommonDrop)attempt.RuleToChain;
			if (child.itemId != ExpectedItems[i] || child.chanceNumerator != 1 ||
				child.chanceDenominator != 9 || child.amountDroppedMinimum <= 0 ||
				child.amountDroppedMaximum < child.amountDroppedMinimum ||
				child.ChainedRules.Count != 0)
				throw Changed($"chain {i} payload");
		}
	}

	private static InvalidOperationException Changed(string detail) =>
		new($"Windy Balloon exact empty-options contract changed: {detail}.");
}
