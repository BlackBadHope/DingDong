using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class CalamityPityItemLootAdapter
{
	private const int ExpectedDirectRoots = 35;
	private static readonly Dictionary<int, ItemLoot> Captured = [];
	internal static int Roots { get; private set; }
	internal static bool Verified { get; private set; }

	internal static void Initialize()
	{
		CalamityPityItemLootManifest.Initialize();
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
		Dictionary<int, int> actual = [];
		int roots = 0;
		foreach ((int itemType, ItemLoot loot) in Captured)
			foreach (IItemDropRule rule in loot.Get(includeGlobalDrops: false))
				if (seen.Add(rule) && CalamityPityDropAdapter.TryNormalize(rule))
				{
					HardLootAudit.Visited();
					actual[itemType] = actual.GetValueOrDefault(itemType) + 1;
					roots++;
				}
		CalamityPityItemLootManifest.AssertExact(actual);
		if (roots != ExpectedDirectRoots)
			throw Changed($"direct roots {roots} != {ExpectedDirectRoots}");
		Roots = roots;
		Verified = true;
	}

	internal static void Reset()
	{
		CalamityPityItemLootManifest.Reset();
		Captured.Clear();
		Roots = 0;
		Verified = false;
	}

	private static InvalidOperationException Changed(string surface) =>
		new($"Calamity pity item-loot coverage changed: {surface}.");
}
