using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class AndroLibBagDifficultyAdapter
{
	private sealed record Replacement(List<IItemDropRule> Rules, int Index,
		IItemDropRule Original, IItemDropRule Hardened);

	private static Type bagRuleType;
	private static MethodInfo rulesGetter;
	private static PropertyInfo startedProperty;
	private static readonly Dictionary<int, ItemLoot> Captured = [];
	private static readonly List<Replacement> Replacements = [];

	internal static void Initialize()
	{
		Mod provider = HardLootProviderGate.Require("androLib", new Version(1, 0, 7, 17),
			"bccfb088-dd08-484a-a0ad-a6454167adc4");
		AndroLibBagCoverage.Initialize(provider);
		bagRuleType = provider.Code.GetType("androLib.Common.ModdedLootBagDropRule", true);
		rulesGetter = bagRuleType.GetMethod("get_myDropRules",
			BindingFlags.Instance | BindingFlags.NonPublic);
		Type system = provider.Code.GetType("androLib.AndroModSystem", true);
		startedProperty = system.GetProperty("StartedPostAddRecipes",
			BindingFlags.Static | BindingFlags.Public);
		if (rulesGetter is null || rulesGetter.IsStatic ||
			rulesGetter.ReturnType != typeof(List<IItemDropRule>) ||
			rulesGetter.GetParameters().Length != 0)
			throw Changed("bag rules getter");
		MethodInfo started = startedProperty?.GetMethod;
		if (startedProperty?.PropertyType != typeof(bool) || started is null ||
			!started.IsStatic || !started.IsPublic || started.GetParameters().Length != 0)
			throw Changed("PostAddRecipes guard");
		Captured.Clear();
		Replacements.Clear();
	}

	internal static void Capture(Item item, ItemLoot loot)
	{
		if (bagRuleType is not null && item.type > ItemID.None)
			Captured[item.type] = loot;
	}

	internal static void NormalizeDeferred()
	{
		if (startedProperty is null || !(bool)startedProperty.GetValue(null))
			throw Changed("PostAddRecipes not complete");
		if (Captured.Count == 0) throw Changed("no captured item loot handles");
		HashSet<IItemDropRule> roots = new(ReferenceEqualityComparer.Instance);
		HashSet<List<IItemDropRule>> lists = new(ReferenceEqualityComparer.Instance);
		Dictionary<int, int> actual = [];
		foreach ((int itemType, ItemLoot loot) in Captured)
			foreach (IItemDropRule rule in loot.Get(false))
				if (rule.GetType() == bagRuleType && roots.Add(rule))
				{
					actual[itemType] = actual.GetValueOrDefault(itemType) + 1;
					NormalizeBag(rule, itemType, lists);
				}
		if (roots.Count == 0) throw Changed("no materialized bag roots");
		AndroLibBagCoverage.Verify(actual);
	}

	private static void NormalizeBag(IItemDropRule root, int itemType,
		HashSet<List<IItemDropRule>> lists)
	{
		HardLootAudit.Visited();
		if (root.ChainedRules.Count != 0) throw Changed("bag root chains");
		HardLootAudit.Protected();
		if (rulesGetter.Invoke(root, null) is not List<IItemDropRule> rules)
			throw Changed("bag rule list");
		if (!lists.Add(rules)) return;
		if (rules.Count == 0)
		{
			HardLootAudit.DropNothingBranchPreserved();
			return;
		}
		for (int index = 0; index < rules.Count; index++)
		{
			IItemDropRule original = rules[index];
			if (original.GetType() == bagRuleType)
			{
				NormalizeBag(original, itemType, lists);
				continue;
			}
			IItemDropRule hardened = HardLootDetachedRuleNormalizer.Normalize(
				original, $"androLib-bag:{itemType}/child:{index}");
			if (ReferenceEquals(original, hardened)) continue;
			rules[index] = hardened;
			Replacements.Add(new Replacement(rules, index, original, hardened));
		}
	}

	internal static void RestoreAndReset()
	{
		List<Exception> failures = [];
		try
		{
			for (int i = Replacements.Count - 1; i >= 0; i--)
				try { Restore(Replacements[i]); }
				catch (Exception error) { failures.Add(error); }
		}
		finally
		{
			Replacements.Clear();
			Captured.Clear();
			bagRuleType = null;
			rulesGetter = null;
			startedProperty = null;
			AndroLibBagCoverage.Reset();
		}
		if (failures.Count != 0)
			throw new AggregateException("androLib bag drops could not be restored.", failures);
	}

	private static void Restore(Replacement replacement)
	{
		if (replacement.Index >= replacement.Rules.Count ||
			!ReferenceEquals(replacement.Rules[replacement.Index], replacement.Hardened))
			throw Changed("bag list changed before restore");
		replacement.Rules[replacement.Index] = replacement.Original;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"androLib bag drop contract changed: {surface}.");
}
