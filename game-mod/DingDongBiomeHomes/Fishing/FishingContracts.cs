using System;

namespace DingDongBiomeHomes.Fishing;

// Stable IDs are persisted; enum numeric values and IDs are append-only.
public enum FishingHabitat : byte
{
	CoastalShoals, OpenOcean, DeepOcean, Abyss, CaveLake, UndergroundRiver
}
public enum FishingMapLayer : byte { Surface, Underground, Cavern, Underworld }
public enum FishingWeather : byte { Clear, Rain, Storm }

public readonly record struct FishingRigSpec(string Id, string NameEn, string NameRu,
	int TargetDepthTiles, int MinimumDepthTiles, int RequiredProgression, int PowerBonus, int Value);

public readonly record struct FishingConditions(FishingHabitat Habitat, FishingMapLayer MapLayer,
	int WaterDepthTiles, int WaterColumnTiles, int FishingPower, int Progression,
	bool Night, FishingWeather Weather, bool BloodMoon, bool FullMoon, string EventId);

public readonly record struct FishingRewardSpec(string ItemId, FishingHabitat Habitat,
	int MinimumWaterDepth, int MaximumWaterDepth, int RequiredProgression,
	int MinimumPower, int Weight, int MinimumStack, int MaximumStack,
	string EventId = "", bool NightOnly = false, bool RainOnly = false, FishingMapLayer? MapLayer = null);
