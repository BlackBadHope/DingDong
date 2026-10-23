using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	public static NpcTownIdentity CreateFallback(string npcKey, HomeBiome biome, HomeArchetype archetype)
	{
		int hash = PositiveHash(npcKey);
		TownRoofStyle roof = (TownRoofStyle)(hash % Enum.GetValues<TownRoofStyle>().Length);
		TownFeature first = (TownFeature)(1 << (hash % 15));
		TownFeature second = (TownFeature)(1 << ((hash / 17 + 5) % 15));
		return new NpcTownIdentity(npcKey, $"Visitor House {hash % 1000:000}", biome, archetype, 31 + hash % 13, 1 + hash / 13 % 3, roof, first | second, (byte)(1 + hash / 31 % 29), "Torch", 25, hash);
	}

	private static Dictionary<string, NpcTownIdentity> BuildEntries()
	{
		Dictionary<string, NpcTownIdentity> entries = new(StringComparer.Ordinal);
		int variant = 0;
		AddForestAlchemyFargoResidents(entries, ref variant);
		AddForestVanillaThoriumResidents(entries, ref variant);
		AddSnowResidents(entries, ref variant);
		AddDesertResidents(entries, ref variant);
		AddJungleResidents(entries, ref variant);
		AddOceanResidents(entries, ref variant);
		AddUndergroundResidents(entries, ref variant);
		AddHallowResidents(entries, ref variant);
		AddMushroomResidents(entries, ref variant);
		AddUnderworldResidents(entries, ref variant);
		if (entries.Count != 61)
			throw new InvalidOperationException($"Authored NPC town manifest must contain exactly 61 entries, found {entries.Count}.");
		return entries;
	}
}
