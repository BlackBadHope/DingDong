using System;
using System.Linq;
using DingDongBiomeHomes.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Fishing.Worldgen;

public sealed partial class FishingWaterWorldSystem
{
	// A twelve-tile shell at an absolute sea level can float over a lower inland
	// surface. Keep the requested relief only where a real bed supports it.
	private static (int Columns, int Lowered, int Skipped, int Unsupported, int Deepest) ShapeOceanColumns(
		bool left, int width, int waterY, int depth, int seed)
	{
		int columns = 0, lowered = 0, skipped = 0, unsupported = 0, actualBottom = waterY + depth;
		bool[] allowed = naturalMask ?? ContinentalPlacementMaskPolicy.CreateNaturalTerrain();
		for (int edge = 10; edge < width; edge += 8)
		{
			int x = left ? edge : Main.maxTilesX - edge - 8;
			int desired = FishingWaterGeometry.OceanFloor(edge, width, waterY, depth, seed);
			int[] floors = new int[8];
			int deepest = desired;
			bool supported = true;
			for (int dx = 0; dx < 8; dx++)
			{
				floors[dx] = FindOceanBed(x + dx, desired, allowed);
				if (floors[dx] < 0) { supported = false; break; }
				deepest = Math.Max(deepest, floors[dx]);
			}
			if (!supported) { unsupported++; continue; }
			int top = Math.Max(20, waterY - 90);
			Rectangle column = new(x, top, 8, deepest + 16 - top);
			// Include the deeper support in the same provider/object admission.
			if (!GenVars.structures.CanPlace(column, allowed, 4) || HasModWall(column)) { skipped++; continue; }
			for (int dx = 0; dx < 8; dx++)
			{
				int floor = floors[dx];
				if (floor > desired) lowered++;
				for (int y = top; y < floor + 16; y++)
				{
					Tile tile = Main.tile[x + dx, y];
					tile.ClearTile(); tile.LiquidAmount = 0;
					if (y >= floor)
					{
						tile.HasTile = true;
						tile.TileType = y < floor + 3 ? TileID.Sand : TileID.HardenedSand;
					}
					else if (y >= waterY)
					{
						tile.LiquidType = LiquidID.Water; tile.LiquidAmount = 255;
						tile.WallType = WallID.None;
					}
				}
			}
			columns++;
			actualBottom = Math.Max(actualBottom, deepest);
		}
		Terraria.ModLoader.ModContent.GetInstance<FishingWaterWorldSystem>().Mod.Logger.Info(
			$"DINGDONG_OCEAN_BED|side={(left ? "left" : "right")}|columns={columns}|loweredCellsX={lowered}|protectedColumns={skipped}|unsupportedColumns={unsupported}|actualBottom={actualBottom}|floatingShellAdded=False");
		return (columns, lowered, skipped, unsupported, actualBottom);
	}

	private static bool HasModWall(Rectangle area)
	{
		for (int x = area.Left; x < area.Right; x++)
		for (int y = area.Top; y < area.Bottom; y++)
			if (Main.tile[x,y].WallType >= WallID.Count) return true;
		return false;
	}

	private static int FindOceanBed(int x, int desired, bool[] allowed)
	{
		int run = 0;
		int limit = Math.Min(Main.maxTilesY - 80, NativeUnderworldSystem.Layer - 100);
		for (int y = Math.Max(20, desired); y < limit; y++)
		{
			Tile tile = Main.tile[x, y];
			bool solid = tile.HasTile && !tile.IsActuated && tile.TileType < allowed.Length &&
				allowed[tile.TileType] && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
			run = solid ? run + 1 : 0;
			// More than the old shell thickness also makes a diagnostic replay
			// reject that shell instead of mistaking it for natural support.
			if (run == 16) return y - 15;
		}
		return -1;
	}

	internal static void ReplayOceanBedForProbe()
	{
		if (!Main.dedServ || Environment.GetEnvironmentVariable("DINGDONG_TERRAIN_PROBE_NPC") != "@terrain-replay" ||
			Environment.GetEnvironmentVariable("DINGDONG_HOUSING_PROBE") != "1" ||
			!System.IO.Path.GetFileName(Main.ActiveWorldFileData.Path).StartsWith("DingDongTerrainProbe-", StringComparison.Ordinal))
			throw new InvalidOperationException("Ocean geometry replay requires an isolated dedicated fixture.");
		int count = 0, totalColumns = 0;
		foreach (FishingWaterRegion region in regions.Where(r => r.Shape == FishingRegionShape.Ocean).ToArray())
		{
			// Invert the exact persisted Bounds.Height = depth + floor(depth/4).
			int depth = region.OceanProfileDepth;
			if (depth <= 0)
			{
				depth = region.Bounds.Height * 4 / 5;
				while (depth + depth / 4 < region.Bounds.Height) depth++;
			}
			if (region.OceanProfileDepth <= 0 && depth + depth / 4 != region.Bounds.Height)
				throw new InvalidOperationException("Persisted ocean depth cannot be recovered exactly.");
			var result = ShapeOceanColumns(region.Bounds.Left < Main.maxTilesX / 2,
				region.Bounds.Width + 10, region.WaterY, depth, region.Seed);
			// The later Calamity Abyss may protect an entire side in this fixture.
			// Do not weaken those reservations to replay an earlier ocean pass.
			totalColumns += result.Columns;
			int index = regions.IndexOf(region);
			regions[index] = region with { OceanProfileDepth = depth, Bounds = new(region.Bounds.X, region.Bounds.Y,
				region.Bounds.Width, Math.Max(region.Bounds.Height, result.Deepest - region.WaterY + 1)) };
			count++;
		}
		if (count != 2) throw new InvalidOperationException("Expected two persisted ocean regions.");
		if (totalColumns == 0) throw new InvalidOperationException("No unprotected ocean columns available in geometry replay.");
		NativeCoastalBiomeSystem.CaptureGeneratedBounds();
	}
}
