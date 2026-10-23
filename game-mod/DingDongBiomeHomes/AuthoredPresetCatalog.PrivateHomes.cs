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
	private static void AddPrivateHomePresets(Dictionary<string, AuthoredPreset> entries)
	{
		// Sanitized, provenance-pinned crops from Ziiio's authored NPC world.
		// They are deliberately catalogued but remain unassigned until housing,
		// placement and user visual gates pass. Public redistribution is forbidden.
		AddPrivateHome(entries, "Ziiio_SteampunkerWorkshop", "steampunker-workshop", 150, 125, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Workshop);
		AddPrivateHome(entries, "Ziiio_WitchDoctorHut", "witch-doctor-hut", 155, 140, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddPrivateHome(entries, "Ziiio_BarberShop", "barber-shop", 115, 95, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Workshop);
		AddPrivateHome(entries, "Ziiio_Tavern", "tavern", 165, 145, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddPrivateHome(entries, "Ziiio_NightClub", "night-club", 175, 115, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Harbor);
		AddPrivateHome(entries, "Ziiio_AirshipHouse", "airship-house", 160, 110, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Harbor);
		AddPrivateHome(entries, "Ziiio_CliffTreeLodge", "cliff-tree-lodge", 210, 245, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddPrivateHome(entries, "Ziiio_SnowLodge", "snow-lodge", 205, 135, AuthoredPlacementMode.TerrainScene, PresetRole.Residence);
		AddPrivateHome(entries, "Ziiio_VerticalTreehouse", "vertical-treehouse", 185, 310, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddPrivateHome(entries, "Ziiio_ForestLakeCottage", "forest-lake-cottage", 185, 140, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddPrivateHome(entries, "Ziiio_CrimsonSpire", "crimson-spire", 300, 290, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddPrivateHome(entries, "Ziiio_WaterLaboratory", "water-laboratory", 250, 175, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Workshop);
		AddPrivateHome(entries, "Ziiio_DesertPavilion", "desert-pavilion", 190, 140, AuthoredPlacementMode.TerrainScene, PresetRole.Residence);
		AddPrivateHome(entries, "Ziiio_JungleWatchtower", "jungle-watchtower", 125, 195, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddPrivateHome(entries, "Ziiio_JunglePortalHouse", "jungle-portal-house", 190, 170, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddPrivateHome(entries, "Ziiio_IndustrialFoundry", "industrial-foundry", 150, 175, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Workshop);
	}

	private static void AddLatePrivateHomePresets(Dictionary<string, AuthoredPreset> entries)
	{
		AddPrivateHome(entries, "Ziiio_EastHousePurple", "east-house-purple", 85, 115, AuthoredPlacementMode.TerrainScene, PresetRole.Decoration | PresetRole.Utility);
		AddPrivateHome(entries, "Ziiio_EastHouseTan", "east-house-tan", 85, 110, AuthoredPlacementMode.TerrainScene, PresetRole.Decoration | PresetRole.Utility);
		AddPrivateHome(entries, "Ziiio_EastWorkshop", "east-workshop", 90, 105, AuthoredPlacementMode.TerrainScene, PresetRole.Decoration | PresetRole.Workshop);
	}
}
