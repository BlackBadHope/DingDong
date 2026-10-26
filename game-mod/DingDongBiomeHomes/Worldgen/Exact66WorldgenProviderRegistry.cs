namespace DingDongBiomeHomes.Worldgen;

internal static class Exact66WorldgenProviderRegistry
{
	internal static readonly Exact66WorldgenProviderContract[] Entries =
	[
		new("AssortedCrazyThings", "1.4.6.4", "cf22c5e9-9d1a-4cef-a0f7-645c027b9509",
			"Soul Harvester Room", Exact66WorldgenPassClass.Natural),
		new("CalamityMod", "2.2.2", "0e63c698-ec72-446e-ac1f-8d01f0ee7ac7",
			"Draedon Structures", Exact66WorldgenPassClass.Natural),
		new("FargowiltasSouls", "1.7.3.9", "535d0777-d3df-4436-ac3f-e1d1fd0ad83b",
			"CursedCoffinArena", Exact66WorldgenPassClass.Natural),
		new("MagicStorage", "0.7.0.11", "06175f1a-32f6-4ff9-922a-8805e8cd9c60",
			"MagicStorage: Automaton Pass", Exact66WorldgenPassClass.Support),
		// Public edition integrates the original ordering/bounds guards in
		// Compatibility.LegacyPack.LocalWorldgenGuards; no external package.
		new("SOTS", "0.25.1.9", "14c19a4a-afb8-49fb-ab76-cd75dff954ac",
			"SOTS: GemStructures", Exact66WorldgenPassClass.Natural),
		new("ThoriumMod", "1.7.2.8", "c3026632-3918-4387-be6d-aa5539117e51",
			"Thorium Mod: Extra Loot", Exact66WorldgenPassClass.Support)
	];
}
