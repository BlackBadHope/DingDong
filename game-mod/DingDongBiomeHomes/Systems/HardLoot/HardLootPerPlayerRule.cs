using System;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal sealed class HardLootPerPlayerRule : DropPerPlayerOnThePlayer
{
	internal HardLootPerPlayerRule(DropPerPlayerOnThePlayer inner, int divisor)
		: base(inner.itemId, Denominator(inner.chanceDenominator, divisor),
			inner.amountDroppedMinimum, inner.amountDroppedMaximum, inner.condition)
	{
		chanceNumerator = inner.chanceNumerator;
		ChainedRules.AddRange(inner.ChainedRules);
	}

	private static int Denominator(int denominator, int divisor) =>
		HardLootPerPlayerRuleFactory.Denominator(denominator, divisor);
}

internal sealed class HardLootLocalPerClientRule : DropLocalPerClientAndResetsNPCMoneyTo0
{
	internal HardLootLocalPerClientRule(DropLocalPerClientAndResetsNPCMoneyTo0 inner,
		int divisor) : base(inner.itemId, Denominator(inner.chanceDenominator, divisor),
			inner.amountDroppedMinimum, inner.amountDroppedMaximum, inner.condition)
	{
		chanceNumerator = inner.chanceNumerator;
		ChainedRules.AddRange(inner.ChainedRules);
	}

	private static int Denominator(int denominator, int divisor) =>
		HardLootPerPlayerRuleFactory.Denominator(denominator, divisor);
}

internal static class HardLootPerPlayerRuleFactory
{
	internal static IItemDropRule Rewrite(IItemDropRule rule, int divisor)
	{
		Type type = rule.GetType();
		if (type == typeof(DropPerPlayerOnThePlayer))
			return new HardLootPerPlayerRule((DropPerPlayerOnThePlayer)rule, divisor);
		if (type == typeof(DropLocalPerClientAndResetsNPCMoneyTo0))
			return new HardLootLocalPerClientRule(
				(DropLocalPerClientAndResetsNPCMoneyTo0)rule, divisor);
		return null;
	}

	internal static int Denominator(int denominator, int divisor)
	{
		if (denominator <= 0 || divisor <= 0)
			throw new InvalidOperationException("Per-player hard-loot chance must stay positive.");
		long result = (long)denominator * divisor;
		if (result > int.MaxValue)
			throw new InvalidOperationException("Per-player hard-loot denominator overflow.");
		return (int)result;
	}
}
