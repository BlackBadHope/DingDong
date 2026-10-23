using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class AssortedCrazyThingsLocalDropAdapter
{
	private sealed record Mutation(ItemDropWithConditionRule Rule,
		int OriginalDenominator, int HardenedDenominator);

	private static Type exactType;
	private static readonly List<Mutation> Mutations = [];

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("AssortedCrazyThings",
			new Version(1, 4, 6, 4), "cf22c5e9-9d1a-4cef-a0f7-645c027b9509");
		exactType = provider.Code.GetType(
			"AssortedCrazyThings.NPCs.DropRules.DropLocalPerClientConditionPerPlayer", true);
		if (!typeof(ItemDropWithConditionRule).IsAssignableFrom(exactType))
			throw Changed("base type");
		Mutations.Clear();
	}

	internal static bool TryNormalize(IItemDropRule rule)
	{
		if (exactType is null || rule.GetType() != exactType) return false;
		if (rule is not ItemDropWithConditionRule drop || drop.condition is null ||
			drop.itemId <= 0 || drop.chanceNumerator <= 0 || drop.chanceDenominator <= 0 ||
			drop.amountDroppedMinimum <= 0 ||
			drop.amountDroppedMaximum < drop.amountDroppedMinimum ||
			rule.ChainedRules.Count != 0)
			throw Changed("rule shape");

		HardLootChanceKind chance = HardLootRuleSemantics.Chance(
			drop.chanceNumerator, drop.chanceDenominator, true);
		if (HardLootRuleSemantics.PreservesGuarantee(chance))
		{
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		if (chance != HardLootChanceKind.Optional) throw Changed("chance");
		int divisor = HardLootClassifier.ExtraDivisor(
			(float)drop.chanceNumerator / drop.chanceDenominator);
		if (divisor == 1)
		{
			HardLootAudit.UltraRare();
			return true;
		}
		int original = drop.chanceDenominator;
		int hardened = HardLootPerPlayerRuleFactory.Denominator(original, divisor);
		drop.chanceDenominator = hardened;
		Mutations.Add(new Mutation(drop, original, hardened));
		HardLootAudit.Wrapped();
		return true;
	}

	internal static void RestoreAndReset()
	{
		List<Exception> failures = [];
		for (int i = Mutations.Count - 1; i >= 0; i--)
		{
			Mutation mutation = Mutations[i];
			if (mutation.Rule.chanceDenominator != mutation.HardenedDenominator)
				failures.Add(Changed("denominator changed before restore"));
			else mutation.Rule.chanceDenominator = mutation.OriginalDenominator;
		}
		Mutations.Clear();
		exactType = null;
		if (failures.Count != 0)
			throw new AggregateException("AssortedCrazyThings drops could not be restored.", failures);
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"AssortedCrazyThings local drop contract changed: {surface}.");
}
