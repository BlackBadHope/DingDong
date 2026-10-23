using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class AndroLibBasicDifficultyAdapter
{
	private static Type ruleType;
	private static FieldInfo itemField;
	private static FieldInfo chanceField;
	private static readonly Dictionary<IItemDropRule, float> Originals =
		new(ReferenceEqualityComparer.Instance);

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("androLib", new Version(1, 0, 7, 17),
			"bccfb088-dd08-484a-a0ad-a6454167adc4");
		ruleType = provider.Code.GetType("androLib.Common.BasicDropRule", true);
		itemField = RequireField("itemID", typeof(int));
		chanceField = RequireField("dropChance", typeof(float));
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
		int itemId = (int)itemField.GetValue(rule);
		float chance = (float)chanceField.GetValue(rule);
		if (itemId <= 0 || chance <= 0f || float.IsNaN(chance) || float.IsInfinity(chance))
			throw Changed("item or chance");
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
		if (reduced <= 0f || reduced >= chance || float.IsNaN(reduced) || float.IsInfinity(reduced))
			throw Changed("positive reduced chance");
		chanceField.SetValue(rule, reduced);
		HardLootAudit.Wrapped();
		return true;
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
			itemField = chanceField = null;
		}
		if (failures.Count != 0)
			throw new AggregateException("androLib basic drops could not be restored.", failures);
	}

	private static FieldInfo RequireField(string name, Type expected)
	{
		FieldInfo field = ruleType.GetField(name, BindingFlags.Instance |
			BindingFlags.Public | BindingFlags.NonPublic);
		if (field?.FieldType != expected) throw Changed(name);
		return field;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"androLib basic drop contract changed: {surface}.");
}
