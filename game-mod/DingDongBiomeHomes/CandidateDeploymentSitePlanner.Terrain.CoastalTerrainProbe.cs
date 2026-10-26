using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	// Shared coastal classification and mainland-support probes used by surface scanning.
	private static bool IsInsideCoastalStrip(int x) =>
		x < CoastalStripWidth || x > Main.maxTilesX - CoastalStripWidth;

	private static bool IsOceanWater(Tile tile) => tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water;

	private static bool IsNaturalSurfaceFloor(Tile tile)
	{
		if (!tile.HasTile || tile.IsActuated || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType] || Main.tileFrameImportant[tile.TileType])
			return false;
		return tile.TileType is TileID.Dirt or TileID.Stone or TileID.Grass or TileID.ClayBlock or TileID.Mud or TileID.JungleGrass or TileID.MushroomGrass or TileID.Sand or TileID.HardenedSand or TileID.Sandstone or TileID.SnowBlock or TileID.IceBlock or TileID.BreakableIce or TileID.CorruptGrass or TileID.Ebonstone or TileID.CrimsonGrass or TileID.Crimstone or TileID.HallowedGrass or TileID.Pearlstone;
	}

	private static bool HasMainlandSupport(int x, int surfaceY)
	{
		int samples = 0;
		int solid = 0;
		for (int sampleX = Math.Max(20, x - 6); sampleX <= Math.Min(Main.maxTilesX - 20, x + 6); sampleX += 3)
		{
			for (int sampleY = surfaceY + 4; sampleY <= Math.Min(Main.maxTilesY - 20, surfaceY + 52); sampleY += 4)
			{
				samples++;
				Tile tile = Main.tile[sampleX, sampleY];
				if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
					solid++;
			}
		}

		return samples > 0 && solid * 100 >= samples * 36;
	}

	// Sky islands have a thin solid cap and then air down to worldSurface.
	// Real hills stay mostly solid between the contact and the global surface.
	private static bool HasDeepMainlandSupport(int x, int surfaceY)
	{
		// The expedition's recorded coastal islands are intended settlement
		// ground. They have a real solid keel but open water beneath it, unlike
		// mainland. Natural-floor, clearance and local-support checks still run
		// in TryFindSurfaceFloor; unrelated floating sky islands remain excluded.
		var ocean = OceanExpeditions.OceanExpeditionWorldSystem.Layout;
		if (ocean != null)
		{
			var island = ocean.IslandSpan(ocean.Edge(x));
			if (island.Top != int.MaxValue && surfaceY >= island.Top &&
				surfaceY <= island.Top + 4 && island.Bottom - surfaceY >= 52)
				return true;
		}
		int worldSurface = (int)Main.worldSurface;
		if (surfaceY >= worldSurface - 24)
			return true;
		int samples = 0;
		int solid = 0;
		int bottom = Math.Min(Main.maxTilesY - 20, Math.Max(surfaceY + 80, worldSurface + 4));
		for (int y = surfaceY + 6; y <= bottom; y += 6)
		{
			samples++;
			if (WorldGen.SolidTile(x, y))
				solid++;
		}

		return samples > 0 && solid * 100 >= samples * 36;
	}

	private static bool HasWaterlineMainlandSupport(int x, int waterlineY)
	{
		int supportedColumns = 0;
		for (int sampleX = Math.Max(20, x - 12); sampleX <= Math.Min(Main.maxTilesX - 20, x + 12); sampleX += 4)
		{
			for (int sampleY = waterlineY + 2; sampleY <= Math.Min(Main.maxTilesY - 20, waterlineY + 96); sampleY += 2)
			{
				if (!IsNaturalSurfaceFloor(Main.tile[sampleX, sampleY]))
					continue;
				if (HasMainlandSupport(sampleX, sampleY))
					supportedColumns++;
				break;
			}
		}

		return supportedColumns >= 3;
	}
}
