using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal static class CalamityPityItemLootManifest
{
	private const int ExpectedRoots = 35;
	private static Dictionary<int, int> expected;

	internal static void Initialize()
	{
		Mod calamity = HardLootProviderGate.Require("CalamityMod", new Version(2, 2, 2),
			"0e63c698-ec72-446e-ac1f-8d01f0ee7ac7");
		expected = new Dictionary<int, int>
		{
			[ItemID.DeerclopsBossBag] = 1,
			[ItemID.QueenBeeBossBag] = 1,
			[ItemID.WallOfFleshBossBag] = 2,
			[ItemID.PlanteraBossBag] = 1,
			[ItemID.GolemBossBag] = 1,
			[ItemID.BossBagBetsy] = 1,
			[ItemID.FishronBossBag] = 1,
			[ItemID.FairyQueenBossBag] = 1,
			[ItemID.MoonLordBossBag] = 1,
		};
		foreach (string name in ModBagNames)
			Add(calamity.Find<ModItem>(name).Type,
				name == "BrimstoneElementalBag" ? 2 : 1);
		if (expected.Count != 33 || Total(expected) != ExpectedRoots)
			throw Changed("expected map");
	}

	internal static void AssertExact(IReadOnlyDictionary<int, int> actual)
	{
		if (expected is null || actual.Count != expected.Count ||
			Total(actual) != ExpectedRoots)
			throw Changed("map size");
		foreach ((int itemType, int count) in expected)
			if (!actual.TryGetValue(itemType, out int actualCount) || actualCount != count)
				throw Changed($"item {itemType}");
	}

	internal static void Reset() => expected = null;

	private static void Add(int itemType, int count)
	{
		if (!expected.TryAdd(itemType, count)) throw Changed($"duplicate item {itemType}");
	}

	private static int Total(IReadOnlyDictionary<int, int> rows)
	{
		int total = 0;
		foreach (int count in rows.Values) total = checked(total + count);
		return total;
	}

	private static readonly string[] ModBagNames =
	[
		"AquaticScourgeBag", "AstrumAureusBag", "AstrumDeusBag",
		"BrimstoneElementalBag", "CalamitasCloneBag", "CalamitasCoffer",
		"CeaselessVoidBag", "CrabulonBag", "CryogenBag", "DesertScourgeBag",
		"DevourerofGodsBag", "DragonfollyBag", "HiveMindBag", "LeviathanBag",
		"OldDukeBag", "PerforatorBag", "PlaguebringerGoliathBag",
		"PolterghastBag", "ProvidenceBag", "RavagerBag", "SignusBag",
		"SlimeGodBag", "StormWeaverBag", "YharonBag"
	];

	private static InvalidOperationException Changed(string surface) =>
		new($"Calamity pity ItemLoot manifest changed: {surface}.");
}
