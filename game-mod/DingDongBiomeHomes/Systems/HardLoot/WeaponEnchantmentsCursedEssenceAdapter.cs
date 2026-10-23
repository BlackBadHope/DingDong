using System;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class WeaponEnchantmentsCursedEssenceAdapter
{
	private static Type ruleType;
	private static Func<float> multiplier;

	internal static void Initialize()
	{
		Mod weapon = HardLootProviderGate.Require("WeaponEnchantments",
			new Version(1, 1, 0, 11), "dbdb60b8-8efe-4b33-aaa1-9c3831f158b3");
		ruleType = weapon.Code.GetType(
			"WeaponEnchantments.Common.CursedEssenceDropRule", true);
		Type config = weapon.Code.GetType(
			"WeaponEnchantments.Common.Configs.ConfigValues", true);
		multiplier = (Func<float>)RequireGetter(config,
			"CursedEssenceDropChanceMultiplier", typeof(float))
			.CreateDelegate(typeof(Func<float>));
		float current = multiplier();
		if (current != 1f || float.IsNaN(current) || float.IsInfinity(current))
			throw Changed("CursedEssenceDropChanceMultiplier must equal 1 for exact66");
	}

	internal static bool TryNormalize(IItemDropRule rule, out IItemDropRule normalized)
	{
		normalized = rule;
		if (rule.GetType() == typeof(WeaponEnchantmentsCursedEssenceRule))
		{
			HardLootAudit.NormalizedPreserved();
			return true;
		}
		if (ruleType is null || rule.GetType() != ruleType) return false;
		if (rule.ChainedRules.Count != 0) throw Changed("CursedEssence chains");
		int divisor = HardLootClassifier.ExtraDivisor(1f / 50f);
		normalized = new WeaponEnchantmentsCursedEssenceRule(rule, divisor, multiplier);
		HardLootAudit.Wrapped();
		return true;
	}

	internal static void Reset()
	{
		ruleType = null;
		multiplier = null;
	}

	private static MethodInfo RequireGetter(Type owner, string name, Type result)
	{
		MethodInfo method = owner.GetProperty(name, BindingFlags.Public |
			BindingFlags.Static)?.GetMethod;
		if (method?.ReturnType != result || method.GetParameters().Length != 0)
			throw Changed(name);
		return method;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"WeaponEnchantments CursedEssence contract changed: {surface}.");
}
