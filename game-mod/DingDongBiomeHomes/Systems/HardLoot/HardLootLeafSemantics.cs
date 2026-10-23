using System;
using Terraria.GameContent.ItemDropRules;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class HardLootLeafSemantics
{
	internal static HardLootChanceKind Inspect(IItemDropRule rule)
	{
		Type type = rule.GetType();
		HardLootChanceKind special = HardLootVanillaSpecialSemantics.Inspect(rule);
		if (special != HardLootChanceKind.Unknown) return special;
		if (type == typeof(ItemDropWithConditionRule))
			return HardLootConditionalCommonSemantics.Inspect(
				(ItemDropWithConditionRule)rule);
		if (type == typeof(CommonDropWithRerolls))
			return HardLootRerollSemantics.Inspect((CommonDropWithRerolls)rule);
		if (IsExactCommon(type)) return InspectCommon((CommonDrop)rule, type);
		if (typeof(CommonDrop).IsAssignableFrom(type)) return HardLootChanceKind.Unsupported;
		if (type == typeof(DropOneByOne))
			return HardLootDropOneByOneSemantics.Inspect((DropOneByOne)rule);
		if (type == typeof(OneFromOptionsDropRule))
		{
			OneFromOptionsDropRule one = (OneFromOptionsDropRule)rule;
			return Options(one.dropIds, one.chanceNumerator, one.chanceDenominator);
		}
		if (type == typeof(OneFromOptionsNotScaledWithLuckDropRule))
		{
			OneFromOptionsNotScaledWithLuckDropRule one =
				(OneFromOptionsNotScaledWithLuckDropRule)rule;
			return Options(one.dropIds, one.chanceNumerator, one.chanceDenominator);
		}
		if (type == typeof(FewFromOptionsDropRule))
		{
			FewFromOptionsDropRule few = (FewFromOptionsDropRule)rule;
			return Options(few.dropIds, few.chanceNumerator, few.chanceDenominator, few.amount);
		}
		if (type == typeof(FewFromOptionsNotScaledWithLuckDropRule))
		{
			FewFromOptionsNotScaledWithLuckDropRule few =
				(FewFromOptionsNotScaledWithLuckDropRule)rule;
			return Options(few.dropIds, few.chanceNumerator, few.chanceDenominator, few.amount);
		}
		if (type == typeof(FromOptionsWithoutRepeatsDropRule))
		{
			FromOptionsWithoutRepeatsDropRule many = (FromOptionsWithoutRepeatsDropRule)rule;
			return HardLootRuleSemantics.ValidIds(many.dropIds) && many.dropCount > 0 &&
				many.dropCount <= many.dropIds.Length
				? HardLootChanceKind.Guaranteed : HardLootChanceKind.Unsupported;
		}
		return HardLootChanceKind.Unknown;
	}

	internal static bool IsExactCommon(Type type) => type == typeof(CommonDrop)
		|| type == typeof(CommonDropNotScalingWithLuck)
		|| type == typeof(CommonDropWithRerolls)
		|| type == typeof(ItemDropWithConditionRule)
		|| type == typeof(DropLocalPerClientAndResetsNPCMoneyTo0)
		|| type == typeof(DropPerPlayerOnThePlayer);

	private static HardLootChanceKind InspectCommon(CommonDrop drop, Type type)
	{
		if (drop.itemId <= 0 || drop.amountDroppedMinimum <= 0 ||
			drop.amountDroppedMaximum < drop.amountDroppedMinimum)
			return HardLootChanceKind.Unsupported;
		bool conditional = type == typeof(ItemDropWithConditionRule)
			|| type == typeof(DropLocalPerClientAndResetsNPCMoneyTo0)
				&& ((DropLocalPerClientAndResetsNPCMoneyTo0)drop).condition is not null
			|| type == typeof(DropPerPlayerOnThePlayer)
				&& ((DropPerPlayerOnThePlayer)drop).condition is not null;
		HardLootChanceKind chance = HardLootRuleSemantics.Chance(
			drop.chanceNumerator, drop.chanceDenominator, conditional);
		return chance;
	}

	private static HardLootChanceKind Options(int[] ids, int numerator,
		int denominator, int amount = 1)
	{
		if (!HardLootRuleSemantics.ValidIds(ids) || amount <= 0 || amount > ids.Length)
			return HardLootChanceKind.Unsupported;
		return HardLootRuleSemantics.Chance(numerator, denominator);
	}
}
