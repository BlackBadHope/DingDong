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
	private static void AddPublicPresets(Dictionary<string, AuthoredPreset> entries)
	{
		Add(entries, "BossArena", 47, 32, 4, 0, "Valkyrienyanko", PresetRole.Utility);
		Add(entries, "Castle1", 67, 43, 15, 3, "Valkyrienyanko", PresetRole.Residence | PresetRole.Civic);
		Add(entries, "FishingPond1", 54, 16, 7, 2, "Valkyrienyanko", PresetRole.Residence | PresetRole.Harbor);
		Add(entries, "ForestTree1", 17, 28, -1, 0, "Expination", PresetRole.Decoration);
		Add(entries, "ForestTree2", 18, 33, -1, 0, "Expination", PresetRole.Decoration);
		Add(entries, "Greenhouse1", 37, 18, 0, 1, "Valkyrienyanko", PresetRole.Residence | PresetRole.Greenhouse);
		Add(entries, "Greenhouse2", 52, 48, 16, 0, "Grim", PresetRole.Greenhouse | PresetRole.Civic);
		Add(entries, "House1_Expination", 39, 27, 1, 2, "Expination", PresetRole.Residence);
		Add(entries, "House2_Expination", 43, 23, 1, 2, "Expination", PresetRole.Residence);
		Add(entries, "House3_Expination", 49, 33, 1, 3, "Expination", PresetRole.Residence);
		Add(entries, "House4_Expination", 61, 30, 1, 1, "Expination", PresetRole.Residence | PresetRole.Workshop, containsNbt: true);
		Add(entries, "JungleFarm", 49, 28, 4, 0, "Toast", PresetRole.Greenhouse | PresetRole.Utility);
		Add(entries, "JungleTree1", 41, 35, -1, 0, "Expination", PresetRole.Decoration);
		Add(entries, "JungleTree2", 28, 31, -1, 0, "Expination", PresetRole.Decoration);
		Add(entries, "LargeHouse1", 34, 24, 6, 3, "Valkyrienyanko", PresetRole.Residence);
		Add(entries, "MediumHouse1", 25, 17, 6, 2, "Valkyrienyanko", PresetRole.Residence);
		Add(entries, "PineTree1", 15, 30, -1, 0, "Expination", PresetRole.Decoration);
		Add(entries, "PineTree2", 15, 30, -1, 0, "Expination", PresetRole.Decoration);
		Add(entries, "PrisonCell1", 5, 17, 0, 1, "Valkyrienyanko", PresetRole.Residence | PresetRole.Utility);
		Add(entries, "PrisonCell2", 12, 5, 0, 1, "Valkyrienyanko", PresetRole.Residence | PresetRole.Utility);
		Add(entries, "PrisonCell3", 7, 9, 0, 1, "Valkyrienyanko", PresetRole.Residence | PresetRole.Utility);
		Add(entries, "PylonCavern", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonDesert", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonForest", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonHallowed", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonJungle", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonMushroom", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonOcean", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonSnow", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
		Add(entries, "PylonUniversal", 7, 6, 0, 0, "Valkyrienyanko", PresetRole.Pylon, containsNbt: true);
	}

	private static void AddLatePublicPresets(Dictionary<string, AuthoredPreset> entries)
	{
		Add(entries, "Ship1", 38, 31, 5, 0, "Valkyrienyanko", PresetRole.Harbor | PresetRole.Civic);
		Add(entries, "SmallHouse1", 19, 15, 5, 1, "Valkyrienyanko", PresetRole.Residence);
		Add(entries, "TowerGate1", 17, 35, 2, 0, "Valkyrienyanko", PresetRole.Civic | PresetRole.Decoration);
		Add(entries, "UndergroundHouse1", 43, 24, 1, 0, "Valkyrienyanko", PresetRole.Civic | PresetRole.Underground);
		Add(entries, "UnderWaterDome", 27, 27, 0, 0, "Valkyrienyanko", PresetRole.Harbor | PresetRole.Underground);
		Add(entries, "WallOfFleshArena", 57, 59, 5, 0, "Valkyrienyanko", PresetRole.Utility);
	}
}
