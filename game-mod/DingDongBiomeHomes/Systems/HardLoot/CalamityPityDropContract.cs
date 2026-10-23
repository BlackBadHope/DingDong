using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal readonly record struct CalamityPityFraction(
	object Value, int Numerator, int Denominator, int StackCount);

internal sealed class CalamityPityDropContract
{
	private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public |
		BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
	private readonly Type ruleType;
	private readonly Type stackType;
	private readonly ConstructorInfo fractionConstructor;
	private readonly FieldInfo dropRateField;
	private readonly FieldInfo stacksField;
	private readonly FieldInfo numeratorField;
	private readonly FieldInfo denominatorField;
	private readonly FieldInfo stackItemField;
	private readonly FieldInfo stackMinimumField;
	private readonly FieldInfo stackMaximumField;

	private CalamityPityDropContract(Mod calamity)
	{
		ruleType = calamity.Code.GetType(
			"CalamityMod.DropHelper+AllOptionsAtOnceWithPityDropRule", true);
		Type fractionType = calamity.Code.GetType("CalamityMod.Fraction", true);
		stackType = calamity.Code.GetType("CalamityMod.WeightedItemStack", true);
		if (ruleType.BaseType != typeof(object) ||
			ruleType.GetFields(Fields).Length != 4 ||
			fractionType.GetFields(Fields).Length != 2 ||
			stackType.GetFields(Fields).Length != 4)
			throw Changed("declared surface");
		dropRateField = ExactField(ruleType, "dropRate", fractionType);
		stacksField = ExactField(ruleType, "stacks", stackType.MakeArrayType());
		ExactField(ruleType, "usesLuck", typeof(bool));
		ExactField(ruleType, "<ChainedRules>k__BackingField",
			typeof(List<IItemDropRuleChainAttempt>));
		numeratorField = ExactField(fractionType, "numerator", typeof(int), true);
		denominatorField = ExactField(fractionType, "denominator", typeof(int), true);
		stackItemField = ExactField(stackType, "itemID", typeof(int));
		ExactField(stackType, "weight", typeof(float));
		stackMinimumField = ExactField(stackType, "minQuantity", typeof(int));
		stackMaximumField = ExactField(stackType, "maxQuantity", typeof(int));
		fractionConstructor = fractionType.GetConstructor([typeof(int), typeof(int)]);
		if (fractionConstructor is null || !fractionConstructor.IsPublic)
			throw Changed("fraction constructor");
	}

	internal static CalamityPityDropContract Load()
	{
		Mod calamity = HardLootProviderGate.Require("CalamityMod", new Version(2, 2, 2),
			"0e63c698-ec72-446e-ac1f-8d01f0ee7ac7");
		return new CalamityPityDropContract(calamity);
	}

	internal bool IsExactRule(IItemDropRule rule) => rule.GetType() == ruleType;

	internal CalamityPityFraction ReadAndValidate(IItemDropRule rule)
	{
		object fraction = dropRateField.GetValue(rule) ?? throw Changed("null fraction");
		int numerator = (int)numeratorField.GetValue(fraction);
		int denominator = (int)denominatorField.GetValue(fraction);
		if (numerator <= 0 || denominator <= 0 || rule.ChainedRules is null ||
			rule.ChainedRules.Count != 0)
			throw Changed("chance or chains");
		int stackCount = ValidateStacks(rule);
		return new CalamityPityFraction(fraction, numerator, denominator, stackCount);
	}

	internal object NewFraction(int numerator, int denominator) =>
		fractionConstructor.Invoke([numerator, denominator]);

	internal void WriteFraction(IItemDropRule rule, object fraction) =>
		dropRateField.SetValue(rule, fraction);

	internal void AssertFraction(IItemDropRule rule, int numerator, int denominator)
	{
		object actual = dropRateField.GetValue(rule) ?? throw Changed("null readback");
		if ((int)numeratorField.GetValue(actual) != numerator ||
			(int)denominatorField.GetValue(actual) != denominator)
			throw Changed("fraction readback");
	}

	private int ValidateStacks(IItemDropRule rule)
	{
		if (stacksField.GetValue(rule) is not Array stacks ||
			stacks.GetType() != stackType.MakeArrayType() || stacks.Length == 0)
			throw Changed("stack array");
		foreach (object stack in stacks)
		{
			int item = (int)stackItemField.GetValue(stack);
			int minimum = (int)stackMinimumField.GetValue(stack);
			int maximum = (int)stackMaximumField.GetValue(stack);
			if (item <= 0 || item >= ItemLoader.ItemCount || minimum <= 0 ||
				maximum < minimum || maximum == int.MaxValue)
				throw Changed("stack entry");
		}
		return stacks.Length;
	}

	private static FieldInfo ExactField(Type owner, string name, Type type,
		bool initOnly = false)
	{
		FieldInfo field = owner.GetField(name, Fields);
		if (field is null || field.FieldType != type || field.IsInitOnly != initOnly)
			throw Changed(name);
		return field;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"Calamity pity-drop contract changed: {surface}.");
}
