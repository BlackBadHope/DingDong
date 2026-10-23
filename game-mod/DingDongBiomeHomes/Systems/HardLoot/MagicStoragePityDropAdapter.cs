using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class MagicStoragePityDropAdapter
{
	private sealed record Mutation(CommonDrop Rule, int Original, int Hardened);
	private static Type exactType;
	private static FieldInfo wrappedField;
	private static FieldInfo strengthField;
	private static readonly List<Mutation> Mutations = [];

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("MagicStorage", new Version(0, 7, 0, 11),
			"06175f1a-32f6-4ff9-922a-8805e8cd9c60");
		exactType = provider.Code.GetType("MagicStorage.Common.DropRules.PityDropsRule", true);
		wrappedField = exactType.GetField("_wrappedRule",
			BindingFlags.Instance | BindingFlags.NonPublic);
		strengthField = exactType.GetField("strength",
			BindingFlags.Instance | BindingFlags.Public);
		if (wrappedField?.FieldType != typeof(CommonDrop) ||
			strengthField?.FieldType != typeof(float)) throw Changed("fields");
		Mutations.Clear();
	}

	internal static bool TryNormalize(IItemDropRule rule)
	{
		if (exactType is null || rule.GetType() != exactType) return false;
		if (rule.ChainedRules.Count != 0 || wrappedField.GetValue(rule) is not CommonDrop drop ||
			!HardLootLeafSemantics.IsExactCommon(drop.GetType()) ||
			drop.ChainedRules.Count != 0 || strengthField.GetValue(rule) is not float strength ||
			float.IsNaN(strength) || float.IsInfinity(strength) || strength < 0f)
			throw Changed("rule shape");
		if (Mutations.Exists(entry => ReferenceEquals(entry.Rule, drop)))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		HardLootChanceKind chance = HardLootRuleSemantics.Inspect(drop);
		if (HardLootRuleSemantics.PreservesGuarantee(chance) ||
			HardLootClassifier.IsProtectedItem(drop.itemId))
		{
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		if (chance != HardLootChanceKind.Optional) throw Changed("chance");
		int divisor = HardLootClassifier.ExtraDivisor(
			(float)drop.chanceNumerator / drop.chanceDenominator);
		if (divisor == 1) { HardLootAudit.UltraRare(); return true; }
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
			Mutation entry = Mutations[i];
			if (entry.Rule.chanceDenominator != entry.Hardened)
				failures.Add(Changed("denominator changed before restore"));
			else entry.Rule.chanceDenominator = entry.Original;
		}
		Mutations.Clear(); exactType = null; wrappedField = strengthField = null;
		if (failures.Count != 0) throw new AggregateException(
			"MagicStorage pity drops could not be restored.", failures);
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"MagicStorage pity-drop contract changed: {surface}.");
}
