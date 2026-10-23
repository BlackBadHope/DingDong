using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Systems.HardLoot;

internal readonly record struct ThoriumMultipleShape(
	int Amount, int Denominator, int Numerator, int Options);

internal static class ThoriumMultipleItemLootManifest
{
	private const int ExpectedRoots = 19;
	private static Dictionary<int, ThoriumMultipleShape> expected;

	internal static void Initialize(Mod thorium)
	{
		expected = [];
		Add(ItemID.GoodieBag, 1, 15, 1, 2);
		Add(ItemID.WallOfFleshBossBag, 1, 1, 1, 3);
		Add(ItemID.PlanteraBossBag, 1, 1, 1, 4);
		Add(ItemID.MoonLordBossBag, 1, 3, 2, 3);
		Add(thorium, "LunaticCultistTreasureBag", 2, 1, 1, 3);
		Add(thorium, "FlyingDutchmanTreasureBag", 1, 1, 1, 5);
		Add(thorium, "MartianSaucerTreasureBag", 1, 1, 1, 4);
		Add(thorium, "ViscountTreasureBag", 2, 1, 1, 7);
		Add(thorium, "GraniteEnergyStormTreasureBag", 1, 1, 1, 5);
		Add(thorium, "StarScouterTreasureBag", 2, 1, 1, 7);
		Add(thorium, "QueenJellyfishTreasureBag", 1, 1, 1, 5);
		Add(thorium, "TheGrandThunderBirdTreasureBag", 1, 1, 1, 4);
		Add(thorium, "BuriedChampionTreasureBag", 1, 1, 1, 5);
		Add(thorium, "ForgottenOneTreasureBag", 2, 1, 1, 6);
		Add(thorium, "FallenBeholderTreasureBag", 2, 1, 1, 7);
		Add(thorium, "BoreanStriderTreasureBag", 2, 1, 1, 5);
		Add(thorium, "LichTreasureBag", 2, 1, 1, 5);
		Add(thorium, "DarkMageTreasureBag", 2, 1, 1, 4);
		Add(thorium, "OgreTreasureBag", 1, 1, 1, 2);
		if (expected.Count != ExpectedRoots || GuaranteedCount() != 17 ||
			OptionalCount() != 2)
			throw Changed("expected manifest");
	}

	internal static void AssertExact(
		IReadOnlyDictionary<int, ThoriumMultipleShape> actual)
	{
		if (expected is null || actual.Count != ExpectedRoots ||
			actual.Count != expected.Count)
			throw Changed($"root count {actual.Count}");
		foreach ((int itemType, ThoriumMultipleShape shape) in expected)
			if (!actual.TryGetValue(itemType, out ThoriumMultipleShape found) ||
				found != shape)
				throw Changed($"item {itemType}");
	}

	internal static void Reset() => expected = null;

	private static void Add(Mod thorium, string name, int amount,
		int denominator, int numerator, int options) =>
		Add(thorium.Find<ModItem>(name).Type, amount, denominator, numerator, options);

	private static void Add(int itemType, int amount, int denominator,
		int numerator, int options)
	{
		if (!expected.TryAdd(itemType,
			new ThoriumMultipleShape(amount, denominator, numerator, options)))
			throw Changed($"duplicate expected item {itemType}");
	}

	private static int GuaranteedCount()
	{
		int count = 0;
		foreach (ThoriumMultipleShape shape in expected.Values)
			if (shape.Numerator >= shape.Denominator) count++;
		return count;
	}

	private static int OptionalCount() => expected.Count - GuaranteedCount();

	private static InvalidOperationException Changed(string surface) =>
		new($"Thorium multiple-rule ItemLoot manifest changed: {surface}.");
}
