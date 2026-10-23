using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Fishing.Encounters;

public enum FishingTimeWindow : byte { Any, Day, Night }
public enum FishingWeatherWindow : byte { Any, Clear, Wet, Storm }

public readonly record struct FishingEventSpec(string Id, string NameEn, string NameRu,
	FishingHabitat Habitat, int RequiredProgression, int MinimumPower,
	int MinimumDepth, int MaximumDepth, FishingTimeWindow Time,
	FishingWeatherWindow Weather, bool FullMoonOnly, int DurationTicks)
{
	public bool CanStart(in FishingConditions c) => c.Habitat == Habitat
		&& c.Progression >= RequiredProgression && c.FishingPower >= MinimumPower
		&& c.WaterDepthTiles >= MinimumDepth && c.WaterDepthTiles <= MaximumDepth
		&& c.WaterColumnTiles >= 2 && !c.BloodMoon
		&& (Time == FishingTimeWindow.Any || (Time == FishingTimeWindow.Night) == c.Night)
		&& (!FullMoonOnly || c.FullMoon)
		&& (Weather == FishingWeatherWindow.Any
			|| (Weather == FishingWeatherWindow.Clear && c.Weather == FishingWeather.Clear)
			|| (Weather == FishingWeatherWindow.Wet && c.Weather != FishingWeather.Clear)
			|| (Weather == FishingWeatherWindow.Storm && c.Weather == FishingWeather.Storm));
}

// Pure policy is shared by the runtime and source-linked offline checks.
public static class FishingEncounterPolicy
{
	public const int HabitatCount = 6;
	public const int ScanInterval = 300;
	public const int CooldownTicks = 90 * 60;
	public const int FishingGraceTicks = 10 * 60;
	public const int MaximumDuration = 5 * 60 * 60;
	public const int MaximumNearbyEnemies = 6;
	public const int MaximumEnemiesOfType = 3;
	public const int MaximumWorldEnemies = 24;
	public static bool IsAuthority(int netMode) => netMode != 1;
	public static bool IsHabitatIndex(int index) => (uint)index < HabitatCount;
	public static float BobberDistanceLimit(int targetDepthTiles) => targetDepthTiles <= 0 ? 900f
		: Math.Max(900f, (Math.Clamp(targetDepthTiles, 0, 512) + 80) * 16f);
	public static bool IsValidWaterTile(int x, int y, int width, int height, int liquidAmount,
		int liquidType, bool hasBlockingTile) => x >= 2 && y >= 2 && x < width - 2
		&& y < height - 2 && liquidAmount >= 128 && liquidType == 0 && !hasBlockingTile;
	public static bool MaySpawn(int netMode, bool wetBobber, bool ownerActive, bool ownerDead,
		bool ownEventActive, int sameTypeCount, int nearbyCount, int worldCount) => IsAuthority(netMode)
		&& wetBobber && ownerActive && !ownerDead && ownEventActive
		&& sameTypeCount < MaximumEnemiesOfType && nearbyCount < MaximumNearbyEnemies
		&& worldCount < MaximumWorldEnemies;
	public static int SanitizeRemaining(int eventIndex, int ticks, FishingHabitat habitat,
		IReadOnlyList<FishingEventSpec> definitions) => eventIndex >= 0 && eventIndex < definitions.Count
		&& definitions[eventIndex].Habitat == habitat ? Math.Clamp(ticks, 0, MaximumDuration) : 0;
	public static bool CanContinue(in FishingEventSpec spec, in FishingConditions conditions) =>
		spec.CanStart(conditions); // Time/weather changes terminate the event at the next bounded scan.
}

public static class FishingEventCatalog
{
	public static IReadOnlyList<FishingEventSpec> Definitions { get; } = Array.AsReadOnly(new[]
	{
		new FishingEventSpec("shoal_run", "Shoal Run", "Ход прибрежной стаи", FishingHabitat.CoastalShoals, 0, 15, 2, 18, FishingTimeWindow.Day, FishingWeatherWindow.Any, false, 180 * 60),
		new FishingEventSpec("tidal_wash", "Tidal Wash", "Приливный вынос", FishingHabitat.CoastalShoals, 1, 30, 4, 24, FishingTimeWindow.Any, FishingWeatherWindow.Wet, false, 150 * 60),
		new FishingEventSpec("feeding_frenzy", "Feeding Frenzy", "Охотничья стая", FishingHabitat.OpenOcean, 1, 40, 8, 60, FishingTimeWindow.Night, FishingWeatherWindow.Any, false, 180 * 60),
		new FishingEventSpec("storm_drift", "Storm Drift", "Штормовой дрейф", FishingHabitat.OpenOcean, 3, 60, 12, 80, FishingTimeWindow.Any, FishingWeatherWindow.Storm, false, 150 * 60),
		new FishingEventSpec("lantern_bloom", "Lantern Bloom", "Пробуждение фонарей", FishingHabitat.DeepOcean, 2, 55, 25, 160, FishingTimeWindow.Night, FishingWeatherWindow.Any, true, 180 * 60),
		new FishingEventSpec("pressure_surge", "Pressure Surge", "Глубинный выброс", FishingHabitat.DeepOcean, 4, 80, 35, 200, FishingTimeWindow.Any, FishingWeatherWindow.Storm, false, 150 * 60),
		new FishingEventSpec("black_current", "Black Current", "Чёрное течение", FishingHabitat.Abyss, 3, 75, 50, 900, FishingTimeWindow.Night, FishingWeatherWindow.Any, false, 150 * 60),
		new FishingEventSpec("leviathan_wake", "Leviathan Wake", "След древней глубины", FishingHabitat.Abyss, 6, 110, 70, 1000, FishingTimeWindow.Night, FishingWeatherWindow.Any, true, 120 * 60),
		new FishingEventSpec("crystal_hatch", "Crystal Hatch", "Выход кристальных игл", FishingHabitat.CaveLake, 1, 30, 8, 80, FishingTimeWindow.Night, FishingWeatherWindow.Any, true, 180 * 60),
		new FishingEventSpec("mineral_bloom", "Mineral Bloom", "Минеральное свечение", FishingHabitat.CaveLake, 3, 55, 12, 120, FishingTimeWindow.Any, FishingWeatherWindow.Wet, false, 180 * 60),
		new FishingEventSpec("flash_flood", "Flash Flood", "Подземный паводок", FishingHabitat.UndergroundRiver, 1, 35, 4, 80, FishingTimeWindow.Any, FishingWeatherWindow.Wet, false, 150 * 60),
		new FishingEventSpec("silt_run", "Silt Run", "Илистый поток", FishingHabitat.UndergroundRiver, 4, 70, 8, 120, FishingTimeWindow.Day, FishingWeatherWindow.Clear, false, 180 * 60)
	});
	public static int IndexOf(string id)
	{
		for (int i = 0; i < Definitions.Count; i++)
			if (String.Equals(Definitions[i].Id, id, StringComparison.Ordinal)) return i;
		return -1;
	}
}
