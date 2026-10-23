using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class CalamityPerPlayerAdapter
{
	private const int ExactProtectionTime = 18000;
	private static Type ruleType;
	private static FieldInfo protectionTimeField;
	private static readonly Dictionary<IItemDropRule, int> Originals =
		new(ReferenceEqualityComparer.Instance);

	internal static void Initialize()
	{
		Mod calamity = HardLootProviderGate.Require("CalamityMod", new Version(2, 2, 2),
			"0e63c698-ec72-446e-ac1f-8d01f0ee7ac7");
		ruleType = calamity.Code.GetType("CalamityMod.DropHelper+PerPlayerDropRule", true);
		if (ruleType.BaseType != typeof(CommonDrop))
			throw Changed("base type");
		FieldInfo[] declared = ruleType.GetFields(BindingFlags.Instance |
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
		if (declared.Length != 1 || declared[0].Name != "protectionTime" ||
			declared[0].FieldType != typeof(int))
			throw Changed("declared fields");
		protectionTimeField = declared[0];
		if (Originals.Count != 0)
			throw Changed("initialize before restore");
	}

	internal static bool TryNormalize(IItemDropRule rule)
	{
		if (ruleType is null || rule.GetType() != ruleType) return false;
		if (Originals.ContainsKey(rule))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		if (rule is not CommonDrop drop ||
			(int)protectionTimeField.GetValue(rule) != ExactProtectionTime)
			throw Changed("instance shape");
		Validate(drop);
		if (drop.chanceNumerator >= drop.chanceDenominator)
		{
			Originals.Add(rule, drop.chanceDenominator);
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		if (rule.ChainedRules.Count != 0)
			throw Changed("optional chained rule");
		float chance = (float)drop.chanceNumerator / drop.chanceDenominator;
		int divisor = HardLootClassifier.ExtraDivisor(chance);
		if (divisor == 1)
		{
			Originals.Add(rule, drop.chanceDenominator);
			HardLootAudit.UltraRare();
			return true;
		}
		int denominator = HardLootPerPlayerRuleFactory.Denominator(
			drop.chanceDenominator, divisor);
		Originals.Add(rule, drop.chanceDenominator);
		drop.chanceDenominator = denominator;
		HardLootAudit.Wrapped();
		return true;
	}

	internal static void RestoreAndReset()
	{
		List<Exception> failures = [];
		try
		{
			foreach ((IItemDropRule rule, int denominator) in Originals)
				try { ((CommonDrop)rule).chanceDenominator = denominator; }
				catch (Exception error) { failures.Add(error); }
		}
		finally
		{
			Originals.Clear();
			ruleType = null;
			protectionTimeField = null;
		}
		if (failures.Count != 0)
			throw new AggregateException("Calamity per-player drops could not be restored.", failures);
	}

	private static void Validate(CommonDrop drop)
	{
		if (drop.itemId <= 0 || drop.itemId >= ItemLoader.ItemCount ||
			drop.chanceNumerator <= 0 || drop.chanceDenominator <= 0 ||
			drop.amountDroppedMinimum <= 0 ||
			drop.amountDroppedMaximum < drop.amountDroppedMinimum ||
			drop.amountDroppedMaximum == int.MaxValue)
			throw Changed("positive item, chance, or stack");
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"Calamity per-player drop contract changed: {surface}.");
}
