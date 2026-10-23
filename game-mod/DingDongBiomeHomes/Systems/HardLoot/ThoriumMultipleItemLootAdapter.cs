using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class ThoriumMultipleItemLootAdapter
{
	private static readonly Dictionary<int, ItemLoot> Captured = [];
	internal static int Roots { get; private set; }
	internal static bool Verified { get; private set; }

	internal static void Initialize()
	{
		Mod thorium = HardLootProviderGate.Require("ThoriumMod", new Version(1, 7, 2, 8),
			"c3026632-3918-4387-be6d-aa5539117e51");
		ThoriumMultipleItemLootManifest.Initialize(thorium);
		Captured.Clear();
		Roots = 0;
		Verified = false;
	}

	internal static void Capture(Item item, ItemLoot loot)
	{
		if (item.type > ItemID.None) Captured[item.type] = loot;
	}

	internal static void NormalizeDeferred()
	{
		if (Captured.Count == 0) throw Changed("no captured item-loot handles");
		HashSet<IItemDropRule> seen = new(ReferenceEqualityComparer.Instance);
		Dictionary<int, ThoriumMultipleShape> actual = [];
		List<IItemDropRule> roots = [];
		foreach ((int itemType, ItemLoot loot) in Captured)
			foreach (IItemDropRule rule in loot.Get(includeGlobalDrops: false))
				if (seen.Add(rule) && ThoriumMultipleRuleAdapter.TryInspect(
					rule, out ThoriumMultipleSnapshot snapshot))
				{
					ThoriumMultipleShape shape = new(snapshot.Amount,
						snapshot.Denominator, snapshot.Numerator, snapshot.Options.Length);
					if (!actual.TryAdd(itemType, shape))
						throw Changed($"duplicate item root {itemType}");
					roots.Add(rule);
				}
		ThoriumMultipleItemLootManifest.AssertExact(actual);
		foreach (IItemDropRule rule in roots)
		{
			if (!ThoriumMultipleRuleAdapter.TryNormalize(rule))
				throw Changed("validated root disappeared");
			HardLootAudit.Visited();
		}
		Roots = actual.Count;
		Verified = true;
	}

	internal static void Reset()
	{
		Captured.Clear();
		ThoriumMultipleItemLootManifest.Reset();
		Roots = 0;
		Verified = false;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"Thorium multiple-rule ItemLoot coverage changed: {surface}.");
}
