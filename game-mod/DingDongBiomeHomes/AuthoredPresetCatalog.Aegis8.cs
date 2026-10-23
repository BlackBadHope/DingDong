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
	private static void AddAegis8Presets(Dictionary<string, AuthoredPreset> entries)
	{
		// Private crops from TerraMonolith's original Aegis8 construction world.
		// This batch deliberately contains sixteen different architectural scenes;
		// no unpainted tutorial copy or neighbouring crop is counted as a preset.
		AddAegis8(entries, "Aegis8_DesertOasisSettlement", "desert-oasis-settlement", 155, 115, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddAegis8(entries, "Aegis8_DesertGothicManor", "desert-gothic-manor", 60, 105, AuthoredPlacementMode.TerrainScene, PresetRole.Residence);
		AddAegis8(entries, "Aegis8_MountainLodge", "mountain-lodge", 125, 95, AuthoredPlacementMode.TerrainScene, PresetRole.Residence);
		AddAegis8(entries, "Aegis8_FloatingPagoda", "floating-pagoda", 155, 130, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Civic);
		AddAegis8(entries, "Aegis8_ForestHillManor", "forest-hill-manor", 120, 125, AuthoredPlacementMode.TerrainScene, PresetRole.Residence);
		AddAegis8(entries, "Aegis8_CavernLakeSmithy", "cavern-lake-smithy", 175, 200, AuthoredPlacementMode.UndergroundPoiScene, PresetRole.Residence | PresetRole.Workshop | PresetRole.Underground);
		AddAegis8(entries, "Aegis8_CavernLakeManor", "cavern-lake-manor", 175, 195, AuthoredPlacementMode.UndergroundPoiScene, PresetRole.Residence | PresetRole.Underground);
		AddAegis8(entries, "Aegis8_SnowCastle", "snow-castle", 215, 390, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddAegis8(entries, "Aegis8_SnowSpire", "snow-spire", 120, 360, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddAegis8(entries, "Aegis8_HallowCliffManor", "hallow-cliff-manor", 185, 240, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddAegis8(entries, "Aegis8_HallowRidgeVillage", "hallow-ridge-village", 230, 290, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
		AddAegis8(entries, "Aegis8_JungleTreeCity", "jungle-tree-city", 270, 350, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Greenhouse);
		AddAegis8(entries, "Aegis8_DesertSkyPalace", "desert-sky-palace", 360, 230, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Civic);
		AddAegis8(entries, "Aegis8_SteampunkOrb", "steampunk-orb", 260, 240, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Workshop);
		AddAegis8(entries, "Aegis8_ForestTreehouse", "forest-treehouse", 250, 270, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddAegis8(entries, "Aegis8_ForestWindmill", "forest-windmill", 170, 180, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Workshop);
	}
}
