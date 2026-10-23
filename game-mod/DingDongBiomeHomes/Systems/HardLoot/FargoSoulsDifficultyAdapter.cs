using System;
using System.Reflection;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class FargoSoulsDifficultyAdapter
{
	private static Type selectorType;
	private static FieldInfo defaultField;
	private static FieldInfo modeField;
	private static Func<bool> modeEnabled;
	private static ConstructorInfo defaultCondition;
	private static ConstructorInfo modeCondition;
	private static Type championType;
	private static FieldInfo championIds;

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("FargowiltasSouls", new Version(1, 7, 3, 9),
			"535d0777-d3df-4436-ac3f-e1d1fd0ad83b");
		selectorType = RequireType(provider, "FargowiltasSouls.Core.ItemDropRules.DropBasedOnEMode");
		defaultField = RequireField(selectorType, "RuleForDefault");
		modeField = RequireField(selectorType, "RuleForEMode");
		Type world = RequireType(provider, "FargowiltasSouls.Core.Systems.WorldSavingSystem");
		MethodInfo getter = world.GetProperty("EternityMode", BindingFlags.Public | BindingFlags.Static)?.GetMethod;
		if (getter?.ReturnType != typeof(bool)) throw ContractChanged("EternityMode getter");
		modeEnabled = (Func<bool>)getter.CreateDelegate(typeof(Func<bool>));
		defaultCondition = RequireCtor(provider,
			"FargowiltasSouls.Core.ItemDropRules.Conditions.NotEModeDropCondition");
		modeCondition = RequireCtor(provider,
			"FargowiltasSouls.Core.ItemDropRules.Conditions.EModeDropCondition");
		championType = RequireType(provider,
			"FargowiltasSouls.Core.ItemDropRules.ChampionEnchDropRule");
		championIds = championType.GetField("DropIds", BindingFlags.Instance |
			BindingFlags.Public | BindingFlags.NonPublic) ?? throw ContractChanged("Champion DropIds");
	}

	internal static bool TryNormalize(IItemDropRule rule, bool preserveBosses,
		out IItemDropRule replacement)
	{
		replacement = null;
		if (rule.GetType() == championType)
		{
			int[] ids = championIds.GetValue(rule) as int[];
			if (ids is null || ids.Length < 3) throw ContractChanged("Champion drop pool");
			if (Array.Exists(ids, id => id <= 0)) throw ContractChanged("Champion item identity");
			HardLootAudit.GuaranteedPreserved();
			return true;
		}
		if (selectorType is null || rule.GetType() != selectorType) return false;
		IItemDropRule defaultRule = defaultField.GetValue(rule) as IItemDropRule;
		IItemDropRule harderRule = modeField.GetValue(rule) as IItemDropRule;
		if (defaultRule is null || harderRule is null) throw ContractChanged("selector branches");
		if (!HardLootDifficultyNormalizer.TryBuild([defaultRule, harderRule], preserveBosses,
			"FargowiltasSouls.EMode", out IItemDropRule[] wrapped))
			throw ContractChanged("unsafe EMode branch semantics");
		replacement = new HardLootModeRule(wrapped[0], wrapped[1], modeEnabled,
			(IItemDropRuleCondition)defaultCondition.Invoke(null),
			(IItemDropRuleCondition)modeCondition.Invoke(null), rule.ChainedRules);
		return true;
	}

	private static Type RequireType(Mod mod, string name) => mod.Code.GetType(name, true);
	private static FieldInfo RequireField(Type type, string name) =>
		type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		?? throw ContractChanged(name);
	private static ConstructorInfo RequireCtor(Mod mod, string name) =>
		RequireType(mod, name).GetConstructor(BindingFlags.Instance | BindingFlags.Public |
			BindingFlags.NonPublic, null, Type.EmptyTypes, null) ?? throw ContractChanged(name);
	private static InvalidOperationException ContractChanged(string surface) =>
		new($"FargowiltasSouls EMode adapter refused changed {surface}.");

	internal static void Reset()
	{
		selectorType = null;
		defaultField = modeField = null;
		modeEnabled = null;
		defaultCondition = modeCondition = null;
		championType = null;
		championIds = null;
	}
}
