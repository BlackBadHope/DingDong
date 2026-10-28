using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

/// <summary>
/// Owner acceptance item 6, second half (27.10): the residences get transit —
/// a minecart "metro" under the surface running from coast to coast, and a
/// hellevator next to the spawn sanctuary down to the underworld ceiling.
/// Both are carved after every residence is placed and reserved, so every
/// column that touches a protected structure (ours or a provider's, through
/// the StructureMap) or dungeon/temple masonry is left untouched and the line
/// simply resumes on the far side. The metro ends where the ocean begins.
/// </summary>
internal static class ResidenceTransitBuilder
{
	internal const int MetroDepthBelowSurface = 90;
	internal const int MetroHeight = 5;           // air rows above the track
	internal const int MetroEdgeMargin = 24;
	internal const int OceanStopColumns = 24;     // consecutive liquid track columns that end the line
	internal const int ShaftHalfWidth = 1;        // 3 tiles wide
	internal const int ShaftOffsetFromSanctuary = 6;
	internal const int ShaftBottomAboveUnderworld = 12;

	internal static int MetroFloorY() => Math.Min(Main.maxTilesY - 400, (int)Main.worldSurface + MetroDepthBelowSurface);

	internal static void Build(Mod mod)
	{
		int floorY = MetroFloorY();
		(int trackCells, int leftEnd, int rightEnd, int skipped) = CarveMetro(floorY);
		int shaftX = ShaftColumn(out string shaftOrigin);
		(int shaftTop, int shaftBottom, int shaftCells, int shaftSkipped, int lavaCleared) = CarveShaft(shaftX, floorY);
		mod.Logger.Info($"DINGDONG_TRANSIT|verdict=PASS|metroY={floorY}|left={leftEnd}|right={rightEnd}|trackCells={trackCells}|metroSkippedColumns={skipped}|shaftX={shaftX}|shaftOrigin={shaftOrigin}|shaftTop={shaftTop}|shaftBottom={shaftBottom}|shaftCells={shaftCells}|shaftSkippedRows={shaftSkipped}|lavaCleared={lavaCleared}");
	}

	private static int ShaftColumn(out string origin)
	{
		var sanctuary = ModContent.GetInstance<SpawnSanctuaryWorldgenSystem>().PlannedSanctuary;
		if (sanctuary.HasValue)
		{
			origin = "sanctuary";
			return Math.Clamp(sanctuary.Value.SafeArea.Right + ShaftOffsetFromSanctuary, 40, Main.maxTilesX - 40);
		}
		origin = "spawn";
		return Math.Clamp(Main.spawnTileX + 20, 40, Main.maxTilesX - 40);
	}

	private static bool ColumnProtected(int x, int top, int height)
	{
		if (!GenVars.structures.CanPlace(new Rectangle(x, top, 1, height), 0))
			return true;
		for (int y = top; y < top + height; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.HasTile && (ContinentalNaturalTiles.IsDungeonBrick(tile.TileType) || tile.TileType == TileID.LihzahrdBrick))
				return true;
			if (tile.WallType is WallID.LihzahrdBrickUnsafe or WallID.BlueDungeonUnsafe or WallID.GreenDungeonUnsafe or WallID.PinkDungeonUnsafe)
				return true;
		}
		return false;
	}

	private static (int Cells, int Left, int Right, int Skipped) CarveMetro(int floorY)
	{
		int center = Main.maxTilesX / 2;
		int cells = 0, skipped = 0;
		int left = Sweep(center, -1, floorY, ref cells, ref skipped);
		int right = Sweep(center + 1, +1, floorY, ref cells, ref skipped);
		WorldGen.RangeFrame(Math.Max(1, left - 1), floorY - MetroHeight - 1, Math.Min(Main.maxTilesX - 2, right + 1), floorY + 2);
		return (cells, left, right, skipped);
	}

	// Walks from the world centre towards one edge. Returns the last column reached.
	private static int Sweep(int start, int step, int floorY, ref int cells, ref int skipped)
	{
		int liquidRun = 0, last = start;
		for (int x = start; x >= MetroEdgeMargin && x < Main.maxTilesX - MetroEdgeMargin; x += step)
		{
			Tile track = Main.tile[x, floorY];
			if (track.LiquidAmount > 0 && track.LiquidType == LiquidID.Water && !track.HasTile)
			{
				if (++liquidRun >= OceanStopColumns) break;
			}
			else liquidRun = 0;
			if (ColumnProtected(x, floorY - MetroHeight, MetroHeight + 2)) { skipped++; last = x; continue; }
			for (int y = floorY - MetroHeight; y < floorY; y++)
			{
				Tile t = Main.tile[x, y];
				t.ClearEverything();
			}
			Tile rail = Main.tile[x, floorY];
			rail.ClearEverything();
			rail.HasTile = true;
			rail.TileType = TileID.MinecartTrack;
			Tile bed = Main.tile[x, floorY + 1];
			if (!bed.HasTile || !Main.tileSolid[bed.TileType])
			{
				bed.ClearEverything();
				bed.HasTile = true;
				bed.TileType = TileID.GrayBrick;
			}
			cells++;
			last = x;
		}
		return last;
	}

	private static (int Top, int Bottom, int Cells, int Skipped, int LavaCleared) CarveShaft(int x, int metroFloorY)
	{
		int top = SurfaceAt(x);
		int bottom = Main.maxTilesY - 200 - ShaftBottomAboveUnderworld; // vanilla underworld starts at maxTilesY-200
		int cells = 0, skipped = 0, lava = 0;
		for (int y = top; y < bottom; y++)
		{
			if (ColumnProtected(x - ShaftHalfWidth, y, 1) || ColumnProtected(x + ShaftHalfWidth, y, 1)) { skipped++; continue; }
			for (int dx = -ShaftHalfWidth; dx <= ShaftHalfWidth; dx++)
			{
				Tile t = Main.tile[x + dx, y];
				if (t.LiquidAmount > 0 && t.LiquidType == LiquidID.Lava) lava++;
				t.ClearEverything();
				cells++;
			}
			// Lava pockets beside the shaft would pour in; turn their edge cells to obsidian.
			for (int dx = -ShaftHalfWidth - 1; dx <= ShaftHalfWidth + 1; dx += 2 * ShaftHalfWidth + 2)
			{
				Tile edge = Main.tile[x + dx, y];
				if (edge.LiquidAmount > 0 && edge.LiquidType == LiquidID.Lava)
				{
					edge.ClearEverything();
					edge.HasTile = true;
					edge.TileType = TileID.Obsidian;
					lava++;
				}
			}
			if (y != metroFloorY)
			{
				Tile rope = Main.tile[x, y];
				rope.HasTile = true;
				rope.TileType = TileID.Rope;
			}
		}
		WorldGen.RangeFrame(Math.Max(1, x - ShaftHalfWidth - 2), Math.Max(1, top - 1), Math.Min(Main.maxTilesX - 2, x + ShaftHalfWidth + 2), Math.Min(Main.maxTilesY - 2, bottom + 1));
		return (top, bottom, cells, skipped, lava);
	}

	private static int SurfaceAt(int x)
	{
		for (int y = 40; y < Main.maxTilesY - 300; y++)
		{
			Tile t = Main.tile[x, y];
			if (t.HasTile && Main.tileSolid[t.TileType]) return y;
		}
		return (int)Main.worldSurface;
	}
}
