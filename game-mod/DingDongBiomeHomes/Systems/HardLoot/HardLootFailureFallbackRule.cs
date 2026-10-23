using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class HardLootFailureFallbackRule : IItemDropRule
{
	private readonly CommonDrop inner;
	private readonly int divisor;

	internal HardLootFailureFallbackRule(CommonDrop inner, int divisor)
	{
		this.inner = inner;
		this.divisor = Math.Max(1, divisor);
		ChainedRules.AddRange(inner.ChainedRules);
	}

	public List<IItemDropRuleChainAttempt> ChainedRules { get; } = [];
	public bool CanDrop(DropAttemptInfo info) => inner.CanDrop(info);

	public ItemDropAttemptResult TryDroppingItem(DropAttemptInfo info)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return new() { State = ItemDropAttemptResultState.DidNotRunCode };
		if (divisor > 1 && info.rng.Next(divisor) != 0)
			return new() { State = ItemDropAttemptResultState.FailedRandomRoll };
		return inner.TryDroppingItem(info);
	}

	public void ReportDroprates(List<DropRateInfo> drops, DropRateInfoChainFeed ratesInfo)
	{
		float rootRate = (float)inner.chanceNumerator / inner.chanceDenominator / divisor;
		drops.Add(new DropRateInfo(inner.itemId, inner.amountDroppedMinimum,
			inner.amountDroppedMaximum, rootRate * ratesInfo.parentDroprateChance,
			ratesInfo.conditions));
		Chains.ReportDroprates(ChainedRules, rootRate, drops, ratesInfo);
	}
}

internal static class HardLootFailureFallbackNormalizer
{
	internal static bool TryNormalize(IItemDropRule rule, int depth,
		out IItemDropRule normalized)
	{
		normalized = rule;
		if (rule.GetType() == typeof(HardLootFailureFallbackRule))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		if (!HardLootChainSemantics.TrySingleGuaranteedFailureFallback(
			rule, depth, out _)) return false;
		CommonDrop common = (CommonDrop)rule;
		Validate(common);
		if (HardLootClassifier.IsProtectedItem(common.itemId))
		{
			HardLootAudit.Protected();
			return true;
		}
		float rate = (float)common.chanceNumerator / common.chanceDenominator;
		int divisor = HardLootClassifier.ExtraDivisor(rate);
		if (divisor == 1)
		{
			HardLootAudit.UltraRare();
			return true;
		}
		normalized = new HardLootFailureFallbackRule(common, divisor);
		HardLootAudit.Wrapped();
		return true;
	}

	private static void Validate(CommonDrop drop)
	{
		if (drop.itemId <= 0 || drop.itemId >= ItemLoader.ItemCount ||
			drop.chanceNumerator <= 0 || drop.chanceDenominator <= 0 ||
			drop.chanceNumerator >= drop.chanceDenominator ||
			drop.amountDroppedMinimum <= 0 ||
			drop.amountDroppedMaximum < drop.amountDroppedMinimum ||
			drop.amountDroppedMaximum == int.MaxValue)
			throw new InvalidOperationException(
				"Guaranteed-fallback root contract changed or is not optional positive.");
	}
}
