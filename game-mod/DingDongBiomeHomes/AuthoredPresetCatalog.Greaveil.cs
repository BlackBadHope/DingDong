using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Immutable inventory of third-party authored geometry embedded in the mod.
/// This is deliberately separate from the NPC manifest: a theme name or a set
/// of procedural parameters is not a building preset.
/// </summary>
public static partial class AuthoredPresetCatalog
{
	private static void AddGreaveilPresets(Dictionary<string, AuthoredPreset> entries)
	{
		// Private Greaveil authored buildings. Later-version decorative IDs were
		// handled only through the audited 1.4.4.9 compatibility whitelist.
		AddGreaveil(entries, "Greaveil_WestGalleon", "west-galleon", 145, 130, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Harbor);
		AddGreaveil(entries, "Greaveil_WestBeachCottage", "west-beach-cottage", 110, 85, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Harbor);
		AddGreaveil(entries, "Greaveil_WestLighthouse", "west-lighthouse", 75, 125, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Harbor);
		AddGreaveil(entries, "Greaveil_DesertPalace", "desert-palace", 160, 110, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddGreaveil(entries, "Greaveil_SnowSteampunkManor", "snow-steampunk-manor", 180, 160, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Workshop);
		AddGreaveil(entries, "Greaveil_SnowLodge", "snow-lodge", 160, 125, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddGreaveil(entries, "Greaveil_HillManor", "hill-manor", 165, 115, AuthoredPlacementMode.TerrainScene, PresetRole.Residence);
		AddGreaveil(entries, "Greaveil_SpawnForestStreet", "spawn-forest-street", 170, 100, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddGreaveil(entries, "Greaveil_SpawnTreeTower", "spawn-tree-tower", 100, 130, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Greenhouse);
		AddGreaveil(entries, "Greaveil_MushroomSpire", "mushroom-spire", 120, 125, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddGreaveil(entries, "Greaveil_JungleWestHouse", "jungle-west-house", 110, 125, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddGreaveil(entries, "Greaveil_JungleEastHouse", "jungle-east-house-restored", AuthoredSourceContext.JungleWidth, AuthoredSourceContext.JungleHeight, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddGreaveil(entries, "Greaveil_UnderworldCastle", "underworld-castle", 360, 190, AuthoredPlacementMode.UnderworldPoiScene, PresetRole.Residence | PresetRole.Civic);
	}
}
