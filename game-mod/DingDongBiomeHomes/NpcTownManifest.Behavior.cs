using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	public static bool TryGet(string npcKey, out NpcTownIdentity identity) => Entries.TryGetValue(npcKey, out identity);
	private static void Add(Dictionary<string, NpcTownIdentity> entries, ref int variant, string npcKey, string theme, HomeBiome biome, HomeArchetype archetype, int width, int floors, TownRoofStyle roof, TownFeature features, byte paint, string shopItemName, int shopPriceSilver)
	{
		NpcTownIdentity identity = new(npcKey, theme, biome, archetype, width, floors, roof, features, paint, shopItemName, shopPriceSilver, variant++);
		if (!entries.TryAdd(npcKey, identity))
			throw new InvalidOperationException($"Duplicate NPC town manifest entry: {npcKey}");
	}

	private static int PositiveHash(string value)
	{
		unchecked
		{
			int hash = 17;
			foreach (char character in value)
				hash = hash * 31 + character;
			return hash & int.MaxValue;
		}
	}
}
