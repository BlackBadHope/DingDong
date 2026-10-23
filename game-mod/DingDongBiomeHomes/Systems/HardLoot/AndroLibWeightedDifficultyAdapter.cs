using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class AndroLibWeightedDifficultyAdapter
{
	private static Type ruleType;
	private static FieldInfo listField;
	private static FieldInfo chanceField;
	private static FieldInfo idField;
	private static FieldInfo weightField;
	private static readonly Dictionary<IItemDropRule, float> Originals =
		new(ReferenceEqualityComparer.Instance);

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("androLib", new Version(1, 0, 7, 17),
			"bccfb088-dd08-484a-a0ad-a6454167adc4");
		ruleType = provider.Code.GetType(
			"androLib.Common.OneFromWeightedOptionsNotScaledWithLuckDropRule", true);
		listField = RequireField(ruleType, "dropsList");
		chanceField = RequireField(ruleType, "dropChance", typeof(float));
		Type[] arguments = listField.FieldType.GetGenericArguments();
		Type pair = arguments.Length == 1 ? arguments[0] : null;
		if (pair?.FullName != "androLib.Common.Utility.WeightedPair")
			throw Changed("weighted pair type");
		idField = RequireField(pair, "ID", typeof(int));
		weightField = RequireField(pair, "Weight", typeof(float));
		Originals.Clear();
	}

	internal static bool TryNormalize(IItemDropRule rule)
	{
		if (ruleType is null || rule.GetType() != ruleType) return false;
		if (Originals.ContainsKey(rule))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		ValidatePool(rule);
		float chance = (float)chanceField.GetValue(rule);
		if (chance <= 0f || float.IsNaN(chance) || float.IsInfinity(chance))
			throw Changed("dropChance");
		if (chance >= 1f)
		{
			Originals.Add(rule, chance);
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		if (rule.ChainedRules.Count != 0) throw Changed("optional chained rule");
		Originals.Add(rule, chance);
		int divisor = HardLootClassifier.ExtraDivisor(chance);
		if (divisor == 1)
		{
			HardLootAudit.UltraRare();
			return true;
		}
		float reduced = chance / divisor;
		if (reduced <= 0f || float.IsNaN(reduced) || float.IsInfinity(reduced))
			throw Changed("positive reduced chance");
		chanceField.SetValue(rule, reduced);
		HardLootAudit.Wrapped();
		return true;
	}

	private static void ValidatePool(IItemDropRule rule)
	{
		if (listField.GetValue(rule) is not IEnumerable options) throw Changed("drop pool");
		int count = 0;
		foreach (object option in options)
		{
			int id = (int)idField.GetValue(option);
			float weight = (float)weightField.GetValue(option);
			if (id <= 0 || weight <= 0f || float.IsNaN(weight) || float.IsInfinity(weight))
				throw Changed("weighted option");
			count++;
		}
		if (count == 0) throw Changed("empty drop pool");
	}

	internal static void RestoreAndReset()
	{
		List<Exception> failures = [];
		try
		{
			if (chanceField is not null)
				foreach ((IItemDropRule rule, float chance) in Originals)
					try { chanceField.SetValue(rule, chance); }
					catch (Exception error) { failures.Add(error); }
		}
		finally
		{
			Originals.Clear();
			ruleType = null;
			listField = chanceField = idField = weightField = null;
		}
		if (failures.Count != 0)
			throw new AggregateException("androLib weighted drops could not be restored.", failures);
	}

	private static FieldInfo RequireField(Type owner, string name, Type expected = null)
	{
		FieldInfo field = owner.GetField(name, BindingFlags.Instance |
			BindingFlags.Public | BindingFlags.NonPublic);
		if (field is null || expected is not null && field.FieldType != expected)
			throw Changed(name);
		return field;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"androLib weighted drop contract changed: {surface}.");
}
