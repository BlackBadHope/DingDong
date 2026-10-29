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
	private static Dictionary<string, AuthoredPreset> BuildEntries()
	{
		Dictionary<string, AuthoredPreset> entries = new(StringComparer.Ordinal);
		AddPublicPresets(entries);
		AddLatePublicPresets(entries);
		AddPrivateHomePresets(entries);
		AddLatePrivateHomePresets(entries);
		AddWorldOfBuildingsPresets(entries);
		AddGreaveilPresets(entries);
		AddAegis8Presets(entries);
		AddImportedPresets(entries);
		AddCatalogPresets(entries);
		return entries;
	}

	private static void AddWorldOfBuildings(Dictionary<string, AuthoredPreset> entries, string id, string fileName, int width, int height, AuthoredPlacementMode placementMode, PresetRole roles)
	{
		AuthoredPreset preset = new(id, $"Structures/PrivateHome/WorldOfBuildings/{fileName}.shstruct", width, height, null, 0, "KIRA and Zilot", WorldOfBuildingsSource, $"wld-sha256:{WorldOfBuildingsWorldSha256}", PrivateHomeLicense, roles | PresetRole.PrivateHome, placementMode, false, false);
		if (!entries.TryAdd(id, preset))
			throw new InvalidOperationException($"Duplicate authored preset id: {id}");
	}

	private static void AddWorldOfBuildingsPresets(Dictionary<string, AuthoredPreset> entries)
	{
		// Runtime-compatible private crops from KIRA and Zilot's building world.
		// Stateful furniture was removed at intake; assignment remains blocked by
		// the same housing/anchor/biome/user visual gates as every other candidate.
		AddWorldOfBuildings(entries, "WoB_HarborWestHouse", "harbor-west-house", 55, 70, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Harbor);
		AddWorldOfBuildings(entries, "WoB_HarborCenterHouse", "harbor-center-house", 55, 80, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Harbor | PresetRole.Civic);
		AddWorldOfBuildings(entries, "WoB_JungleTreehouse", "jungle-treehouse", 130, 130, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Greenhouse);
		AddWorldOfBuildings(entries, "WoB_DesertPalace", "desert-palace", 115, 90, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic);
	}
}
