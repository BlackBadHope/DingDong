using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal readonly record struct ThoriumMultipleSnapshot(
	IItemDropRule[] Options, int Denominator, int Numerator, int Amount);

internal sealed class ThoriumMultipleRuleContract
{
	private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public |
		BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
	private readonly Type ruleType;
	private readonly FieldInfo optionsField;
	private readonly FieldInfo denominatorField;
	private readonly FieldInfo numeratorField;
	private readonly FieldInfo amountField;

	private ThoriumMultipleRuleContract(Mod provider)
	{
		ruleType = provider.Code.GetType(
			"ThoriumMod.Core.ItemDropRules.MultipleFromRulesRule", true);
		if (ruleType.BaseType != typeof(object) ||
			!typeof(INestedItemDropRule).IsAssignableFrom(ruleType) ||
			ruleType.GetFields(Fields).Length != 5)
			throw Changed("declared surface");
		optionsField = ExactField("options", typeof(IItemDropRule[]));
		denominatorField = ExactField("chanceDenominator", typeof(int));
		numeratorField = ExactField("chanceNumerator", typeof(int));
		amountField = ExactField("amount", typeof(int));
		ExactField("<ChainedRules>k__BackingField",
			typeof(List<IItemDropRuleChainAttempt>));
		ConstructorInfo[] constructors = ruleType.GetConstructors(
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		if (constructors.Length != 2 ||
			!HasSignature(constructors, [typeof(int), typeof(int), typeof(IItemDropRule[])]) ||
			!HasSignature(constructors,
				[typeof(int), typeof(int), typeof(int), typeof(IItemDropRule[])]))
			throw Changed("constructors");
	}

	internal static ThoriumMultipleRuleContract Load()
	{
		Mod provider = HardLootProviderGate.Require("ThoriumMod", new Version(1, 7, 2, 8),
			"c3026632-3918-4387-be6d-aa5539117e51");
		return new ThoriumMultipleRuleContract(provider);
	}

	internal bool IsExactRule(IItemDropRule rule) => rule.GetType() == ruleType;

	internal ThoriumMultipleSnapshot ReadAndValidate(IItemDropRule rule)
	{
		if (optionsField.GetValue(rule) is not IItemDropRule[] options ||
			options.Length == 0 || Array.Exists(options, option => option is null) ||
			rule.ChainedRules is null || rule.ChainedRules.Count != 0)
			throw Changed("options or chains");
		int denominator = (int)denominatorField.GetValue(rule);
		int numerator = (int)numeratorField.GetValue(rule);
		int amount = (int)amountField.GetValue(rule);
		if (denominator <= 0 || numerator <= 0 || amount <= 0 || amount > options.Length)
			throw Changed("chance or amount");
		return new ThoriumMultipleSnapshot(options, denominator, numerator, amount);
	}

	internal void WriteAndAssert(IItemDropRule rule, int denominator)
	{
		denominatorField.SetValue(rule, denominator);
		if ((int)denominatorField.GetValue(rule) != denominator)
			throw Changed("denominator readback");
	}

	private FieldInfo ExactField(string name, Type type)
	{
		FieldInfo field = ruleType.GetField(name, Fields);
		if (field is null || field.FieldType != type || field.IsInitOnly || field.IsStatic)
			throw Changed(name);
		return field;
	}

	private static bool HasSignature(ConstructorInfo[] constructors, Type[] signature)
	{
		foreach (ConstructorInfo constructor in constructors)
		{
			ParameterInfo[] parameters = constructor.GetParameters();
			if (parameters.Length != signature.Length) continue;
			bool same = true;
			for (int i = 0; i < signature.Length; i++)
				same &= parameters[i].ParameterType == signature[i];
			if (same) return true;
		}
		return false;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"Thorium multiple-rule contract changed: {surface}.");
}
