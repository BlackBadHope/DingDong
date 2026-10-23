using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class WeaponEnchantmentsDifficultyAdapter
{
	private static Type ruleType;
	private static FieldInfo netIdField;
	private static FieldInfo chanceField;
	private static readonly Dictionary<IItemDropRule, float> Originals =
		new(ReferenceEqualityComparer.Instance);

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("WeaponEnchantments",
			new Version(1, 1, 0, 11), "dbdb60b8-8efe-4b33-aaa1-9c3831f158b3");
		ruleType = provider.Code.GetType("WeaponEnchantments.Common.PowerBoosterDropRule", true);
		netIdField = RequireField("netID", typeof(int));
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
		int netId = (int)netIdField.GetValue(rule);
		float chance = (float)chanceField.GetValue(rule);
		if (netId <= 0 || chance <= 0f || float.IsNaN(chance) || float.IsInfinity(chance))
			throw new InvalidOperationException("WeaponEnchantments PowerBooster chance contract changed.");
		if (chance >= 1f)
		{
			Originals.Add(rule, chance);
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		if (rule.ChainedRules.Count != 0)
			throw new InvalidOperationException(
				"WeaponEnchantments optional PowerBooster chain contract changed.");
		Originals.Add(rule, chance);
		int divisor = HardLootClassifier.ExtraDivisor(chance);
		if (divisor == 1)
		{
			HardLootAudit.UltraRare();
			return true;
		}
		float reduced = chance / divisor;
		if (reduced <= 0f || float.IsNaN(reduced) || float.IsInfinity(reduced))
			throw new InvalidOperationException("WeaponEnchantments PowerBooster chance became invalid.");
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
			netIdField = chanceField = null;
		}
		if (failures.Count != 0)
			throw new AggregateException(
				"WeaponEnchantments drops could not be restored.", failures);
	}

	private static FieldInfo RequireField(string name, Type expected)
	{
		FieldInfo field = ruleType.GetField(name, BindingFlags.Instance |
			BindingFlags.Public | BindingFlags.NonPublic);
		if (field?.FieldType != expected)
			throw new InvalidOperationException($"WeaponEnchantments PowerBooster {name} changed.");
		return field;
	}
}
