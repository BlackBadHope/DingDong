using System;
using DingDongBiomeHomes.Fishing.Encounters;
using DingDongBiomeHomes.Fishing.Worldgen;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes.Fishing;

public readonly record struct FishingWaterColumn(int SurfaceY, int BottomY, int Depth, int Height);

public static class FishingEnvironment
{
	public static int Progression => NPC.downedMoonlord ? 6 : NPC.downedPlantBoss ? 5 :
		NPC.downedMechBossAny ? 4 : Main.hardMode ? 3 : NPC.downedBoss3 ? 2 : NPC.downedBoss2 ? 1 : 0;

	public static FishingConditions At(Player player, FishingAttempt attempt) =>
		AtPosition(attempt.X, attempt.Y, attempt.fishingLevel);

	public static FishingConditions AtPosition(int x, int y, int fishingPower = 0)
	{
		FishingWaterColumn water = Measure(x, y);
		var layer = FishingPolicy.MapLayer(y, Main.worldSurface, Main.rockLayer, global::DingDongBiomeHomes.Worldgen.NativeUnderworldSystem.Layer);
		FishingHabitat habitat;
		if (!FishingWaterWorldSystem.TryGetHabitat(x, y, out habitat))
		{
			// Existing worlds get fishing without terrain edits. Expanded coast uses saved metadata.
			bool ocean = Math.Min(x, Main.maxTilesX - 1 - x) < 380;
			habitat = ocean ? FishingPolicy.OceanHabitat(Math.Min(x, Main.maxTilesX - 1 - x), 380, water.Depth, water.Height) : FishingHabitat.CaveLake;
			if (FishingCalamityExpansion.IsAbyssPosition(x, y)) habitat = FishingHabitat.Abyss;
		}
		var weather = Main.raining ? Main.maxRaining >= 0.6f && Math.Abs(Main.windSpeedCurrent) >= 0.4f ?
			FishingWeather.Storm : FishingWeather.Rain : FishingWeather.Clear;
		return new FishingConditions(habitat, layer, water.Depth, water.Height, Math.Max(0, fishingPower),
			Progression, !Main.dayTime, weather, Main.bloodMoon, Main.moonPhase == 0,
			FishingEncounterSystem.GetEventId(habitat) ?? string.Empty);
	}

	// A bounded vertical scan of one connected water column, not distance from sea level.
	public static FishingWaterColumn Measure(int x, int y)
	{
		if (!WorldGen.InWorld(x, y, 2)) return default;
		if (!WaterAt(x, y))
		{
			if (WaterAt(x, y + 1)) y++;
			else return new FishingWaterColumn(y, y, 0, 0);
		}
		int top = y, bottom = y;
		while (top > 2 && WaterAt(x, top - 1)) top--;
		while (bottom < Main.maxTilesY - 3 && WaterAt(x, bottom + 1)) bottom++;
		return new FishingWaterColumn(top, bottom, Math.Max(0, y - top), bottom - top + 1);
	}

	public static bool WaterAt(int x, int y)
	{
		if (!WorldGen.InWorld(x, y, 1)) return false;
		Tile t = Main.tile[x, y];
		return t.LiquidAmount >= 32 && t.LiquidType == LiquidID.Water &&
			(!t.HasUnactuatedTile || !Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]);
	}
}
