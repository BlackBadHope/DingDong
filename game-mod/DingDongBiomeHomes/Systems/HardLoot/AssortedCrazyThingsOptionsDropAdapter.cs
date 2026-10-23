using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class AssortedCrazyThingsOptionsDropAdapter
{
	private sealed record Mutation(IItemDropRule Rule, int Original, int Hardened);
	private static Type exactType;
	private static FieldInfo idsField, denominatorField, numeratorField, minimumField, maximumField;
	private static readonly List<Mutation> Mutations = [];

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("AssortedCrazyThings",
			new Version(1, 4, 6, 4), "cf22c5e9-9d1a-4cef-a0f7-645c027b9509");
		exactType = provider.Code.GetType(
			"AssortedCrazyThings.NPCs.DropRules.OneFromOptionsPerPlayerOnPlayerRule", true);
		idsField = Field("dropIds", typeof(int[]));
		denominatorField = Field("chanceDenominator", typeof(int));
		numeratorField = Field("chanceNumerator", typeof(int));
		minimumField = Field("amountDroppedMinimum", typeof(int));
		maximumField = Field("amountDroppedMaximum", typeof(int));
		Mutations.Clear();
	}

	internal static bool TryNormalize(IItemDropRule rule)
	{
		if (exactType is null || rule.GetType() != exactType) return false;
		if (rule.ChainedRules.Count != 0 || idsField.GetValue(rule) is not int[] ids ||
			!HardLootRuleSemantics.ValidIds(ids) ||
			numeratorField.GetValue(rule) is not int numerator || numerator <= 0 ||
			denominatorField.GetValue(rule) is not int denominator || denominator <= 0 ||
			minimumField.GetValue(rule) is not int minimum || minimum <= 0 ||
			maximumField.GetValue(rule) is not int maximum || maximum < minimum)
			throw Changed("rule shape");
		if (Mutations.Exists(entry => ReferenceEquals(entry.Rule, rule)))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		HardLootChanceKind chance = HardLootRuleSemantics.Chance(numerator, denominator);
		if (HardLootRuleSemantics.PreservesGuarantee(chance))
		{
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		if (chance != HardLootChanceKind.Optional) throw Changed("chance");
		int divisor = HardLootClassifier.ExtraDivisor((float)numerator / denominator);
		if (divisor == 1) { HardLootAudit.UltraRare(); return true; }
		int hardened = HardLootPerPlayerRuleFactory.Denominator(denominator, divisor);
		denominatorField.SetValue(rule, hardened);
		Mutations.Add(new Mutation(rule, denominator, hardened));
		HardLootAudit.Wrapped();
		return true;
	}

	internal static void RestoreAndReset()
	{
		List<Exception> failures = [];
		for (int i = Mutations.Count - 1; i >= 0; i--)
		{
			Mutation entry = Mutations[i];
			if (!Equals(denominatorField.GetValue(entry.Rule), entry.Hardened))
				failures.Add(Changed("denominator changed before restore"));
			else denominatorField.SetValue(entry.Rule, entry.Original);
		}
		Mutations.Clear(); exactType = null;
		idsField = denominatorField = numeratorField = minimumField = maximumField = null;
		if (failures.Count != 0) throw new AggregateException(
			"AssortedCrazyThings option drops could not be restored.", failures);
	}

	private static FieldInfo Field(string name, Type type)
	{
		FieldInfo field = exactType.GetField(name, BindingFlags.Instance | BindingFlags.Public);
		return field?.FieldType == type ? field : throw Changed("field " + name);
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"AssortedCrazyThings option-drop contract changed: {surface}.");
}
