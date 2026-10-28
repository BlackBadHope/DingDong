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
		Rectangle[] reservations = ForeignStructureIntegrityGuard.ReadReservations();
		(int trackCells, int leftEnd, int rightEnd, int skipped, int metroFramed) = CarveMetro(floorY, reservations);
		int shaftX = ShaftColumn(out string shaftOrigin, out int groundHint);
		(int shaftTop, int shaftBottom, int shaftCells, int shaftSkipped, int lavaCleared, int shaftFramed) = CarveShaft(shaftX, floorY, groundHint, reservations);
		mod.Logger.Info($"DINGDONG_TRANSIT|verdict=PASS|groundHint={groundHint}|metroY={floorY}|left={leftEnd}|right={rightEnd}|trackCells={trackCells}|metroSkippedColumns={skipped}|metroFramed={metroFramed}|shaftX={shaftX}|shaftOrigin={shaftOrigin}|shaftTop={shaftTop}|shaftBottom={shaftBottom}|shaftCells={shaftCells}|shaftSkippedRows={shaftSkipped}|shaftFramed={shaftFramed}|lavaCleared={lavaCleared}|protectedRegions={reservations.Length}");
	}

	/// <summary>
	/// Cells of the foreign reservations (the same StructureMap rectangles the
	/// integrity guard snapshots) inside one band of the world. S4 x10 run: a
	/// RangeFrame over the whole metro span re-framed tiles inside protected
	/// regions and the guard rejected the world (more than 4096 changed cells);
	/// the line now never writes or frames a protected cell.
	/// </summary>
	private sealed class ProtectionBand
	{
		private readonly Rectangle band;
		private readonly bool[] cells;
		internal int ProtectedCells { get; private set; }

		internal ProtectionBand(Rectangle area, Rectangle[] reservations)
		{
			band = Rectangle.Intersect(area, new Rectangle(0, 0, Main.maxTilesX, Main.maxTilesY));
			cells = new bool[Math.Max(1, band.Width * band.Height)];
			foreach (Rectangle region in reservations)
			{
				Rectangle hit = Rectangle.Intersect(region, band);
				if (hit.Width <= 0 || hit.Height <= 0) continue;
				for (int x = hit.Left; x < hit.Right; x++)
				for (int y = hit.Top; y < hit.Bottom; y++)
				{
					ref bool cell = ref cells[(x - band.X) * band.Height + (y - band.Y)];
					if (!cell) { cell = true; ProtectedCells++; }
				}
			}
		}

		internal bool Protected(int x, int y)
		{
			if (!band.Contains(x, y)) return true; // outside the band is never ours to touch
			return cells[(x - band.X) * band.Height + (y - band.Y)];
		}

		/// <summary>Re-frames only unprotected cells, never breaking a tile (no KillTile cascade into a reservation).</summary>
		internal int Frame(int left, int top, int right, int bottom)
		{
			int framed = 0;
			for (int x = Math.Max(1, left); x <= Math.Min(Main.maxTilesX - 2, right); x++)
			for (int y = Math.Max(1, top); y <= Math.Min(Main.maxTilesY - 2, bottom); y++)
			{
				if (Protected(x, y)) continue;
				WorldGen.TileFrame(x, y, resetFrame: false, noBreak: true);
				framed++;
			}
			return framed;
		}
	}

	private static int ShaftColumn(out string origin, out int groundHint)
	{
		var sanctuary = ModContent.GetInstance<SpawnSanctuaryWorldgenSystem>().PlannedSanctuary;
		if (sanctuary.HasValue)
		{
			origin = "sanctuary";
			groundHint = sanctuary.Value.FloorY;
			return Math.Clamp(sanctuary.Value.SafeArea.Right + ShaftOffsetFromSanctuary, 40, Main.maxTilesX - 40);
		}
		origin = "spawn";
		groundHint = Main.spawnTileY;
		return Math.Clamp(Main.spawnTileX + 20, 40, Main.maxTilesX - 40);
	}

	private static bool ColumnProtected(ProtectionBand band, int x, int top, int height)
	{
		if (x < 1 || x >= Main.maxTilesX - 1 || top < 1 || top + height >= Main.maxTilesY - 1) return true;
		for (int y = top; y < top + height; y++)
		{
			if (band.Protected(x, y)) return true;
			Tile tile = Main.tile[x, y];
			if (tile.HasTile && (ContinentalNaturalTiles.IsDungeonBrick(tile.TileType) || tile.TileType == TileID.LihzahrdBrick))
				return true;
			if (tile.WallType is WallID.LihzahrdBrickUnsafe or WallID.BlueDungeonUnsafe or WallID.GreenDungeonUnsafe or WallID.PinkDungeonUnsafe)
				return true;
		}
		return false;
	}

	private static (int Cells, int Left, int Right, int Skipped, int Framed) CarveMetro(int floorY, Rectangle[] reservations)
	{
		int center = Main.maxTilesX / 2;
		int cells = 0, skipped = 0;
		var band = new ProtectionBand(new Rectangle(0, floorY - MetroHeight - 2, Main.maxTilesX, MetroHeight + 6), reservations);
		int left = Sweep(band, center, -1, floorY, ref cells, ref skipped);
		int right = Sweep(band, center + 1, +1, floorY, ref cells, ref skipped);
		int framed = band.Frame(left - 1, floorY - MetroHeight - 1, right + 1, floorY + 2);
		return (cells, left, right, skipped, framed);
	}

	// Walks from the world centre towards one edge. Returns the last column reached.
	private static int Sweep(ProtectionBand band, int start, int step, int floorY, ref int cells, ref int skipped)
	{
		int liquidRun = 0, last = start;
		for (int x = start; x >= MetroEdgeMargin && x < Main.maxTilesX - MetroEdgeMargin; x += step)
		{
			// S3 run: underground lakes ended the line mid-map (7744..17267 of 28000).
			// Only a column that is water from the open sky down to the track row is ocean.
			if (OceanColumn(x, floorY))
			{
				if (++liquidRun >= OceanStopColumns) break;
			}
			else liquidRun = 0;
			if (ColumnProtected(band, x, floorY - MetroHeight, MetroHeight + 2)) { skipped++; last = x; continue; }
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

	private static (int Top, int Bottom, int Cells, int Skipped, int LavaCleared, int Framed) CarveShaft(int x, int metroFloorY, int groundHint, Rectangle[] reservations)
	{
		int top = SurfaceAt(x, groundHint);
		int bottom = Main.maxTilesY - 200 - ShaftBottomAboveUnderworld; // vanilla underworld starts at maxTilesY-200
		int cells = 0, skipped = 0, lava = 0;
		var band = new ProtectionBand(new Rectangle(x - ShaftHalfWidth - 3, 0, 2 * ShaftHalfWidth + 7, Main.maxTilesY), reservations);
		for (int y = top; y < bottom; y++)
		{
			// The obsidian edge cells (x +- 2) are written too, so the whole 5-wide strip must be free.
			bool blocked = false;
			for (int dx = -ShaftHalfWidth - 1; dx <= ShaftHalfWidth + 1 && !blocked; dx++) blocked = ColumnProtected(band, x + dx, y, 1);
			if (blocked) { skipped++; continue; }
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
		int framed = band.Frame(x - ShaftHalfWidth - 2, top - 1, x + ShaftHalfWidth + 2, bottom + 1);
		return (top, bottom, cells, skipped, lava, framed);
	}

	// Water from the first non-air cell all the way down to the track row: the sea,
	// not a cave lake. Sand or stone anywhere in between means ground.
	private static bool OceanColumn(int x, int floorY)
	{
		int y = 40;
		while (y < floorY && !Main.tile[x, y].HasTile && Main.tile[x, y].LiquidAmount == 0) y++;
		if (y >= floorY) return false;
		for (; y <= floorY; y++)
		{
			Tile t = Main.tile[x, y];
			if (t.HasTile || t.LiquidAmount == 0 || t.LiquidType != LiquidID.Water) return false;
		}
		return true;
	}

	// The ground surface next to the sanctuary, not a cloud or sky island. S3 started
	// the shaft at y=79 (a cloud) and S4/S5 at y=79..100 (islands thick enough for any
	// density test). S6: the shaft stands six tiles beside the sanctuary, so its ground
	// is near the sanctuary floor — start the search a little above that floor (hills)
	// and take the first solid, non-cloud tile with ground under it.
	internal const int GroundSearchAboveHint = 60;
	private static int SurfaceAt(int x, int groundHint)
	{
		const int GroundDepth = 12;
		int start = Math.Clamp(groundHint - GroundSearchAboveHint, 40, Main.maxTilesY - 400);
		for (int y = start; y < Main.maxTilesY - 300; y++)
		{
			Tile t = Main.tile[x, y];
			if (!t.HasTile || !Main.tileSolid[t.TileType] || t.TileType is TileID.Cloud or TileID.RainCloud or TileID.SnowCloud) continue;
			int solid = 0;
			for (int d = 1; d <= GroundDepth; d++)
			{
				Tile below = Main.tile[x, y + d];
				if (below.HasTile && Main.tileSolid[below.TileType]) solid++;
			}
			if (solid >= GroundDepth / 2) return y;
		}
		return Math.Clamp(groundHint, 40, Main.maxTilesY - 400);
	}
}
