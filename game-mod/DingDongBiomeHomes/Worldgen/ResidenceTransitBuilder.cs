using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
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
		var metroObjects = new ObjectLedger();
		var shaftObjects = new ObjectLedger();
		(int trackCells, int leftEnd, int rightEnd, int skipped, int metroFramed) = CarveMetro(floorY, reservations, metroObjects);
		int shaftX = ShaftColumn(out string shaftOrigin, out int groundHint);
		(int shaftTop, int shaftBottom, int shaftCells, int shaftSkipped, int lavaCleared, int shaftFramed, int trackKept, int bedPlatforms) = CarveShaft(shaftX, floorY, groundHint, reservations, shaftObjects);
		mod.Logger.Info($"DINGDONG_TRANSIT|verdict=PASS|groundHint={groundHint}|metroY={floorY}|left={leftEnd}|right={rightEnd}|trackCells={trackCells}|metroSkippedColumns={skipped}|metroFramed={metroFramed}|shaftX={shaftX}|shaftOrigin={shaftOrigin}|shaftTop={shaftTop}|shaftBottom={shaftBottom}|shaftCells={shaftCells}|shaftSkippedRows={shaftSkipped}|shaftFramed={shaftFramed}|lavaCleared={lavaCleared}|protectedRegions={reservations.Length}|shaftTrackKept={trackKept}|shaftBedPlatforms={bedPlatforms}|metroObjectsCleared={metroObjects.Cleared}|metroObjectsKept={metroObjects.Kept}|shaftObjectsCleared={shaftObjects.Cleared}|shaftObjectsKept={shaftObjects.Kept}|chestsDestroyed={metroObjects.Chests + shaftObjects.Chests}|tileEntitiesRemoved={metroObjects.Entities + shaftObjects.Entities}|objectRingFramed={metroObjects.Framed + shaftObjects.Framed}");
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

	/// <summary>
	/// S5 review F6: the carve envelopes used to clear cell by cell, leaving half
	/// pots, chests, altars, statues and mod furniture at their edge plus orphaned
	/// Main.chest records and tile entities. Multi-tile objects are now resolved
	/// whole: removed with their chest/entity when no footprint cell is protected,
	/// otherwise left intact (the carve skips their cells).
	/// </summary>
	private sealed class ObjectLedger
	{
		internal int Cleared, Kept, Chests, Entities, Framed;
	}

	internal const int MaxObjectSide = 16; // larger footprints are never resolved here: left whole

	// Same reservation/masonry rule as ColumnProtected, for one cell (same bounds).
	private static bool CellProtected(ProtectionBand band, int x, int y)
	{
		if (x < 1 || x >= Main.maxTilesX - 1 || y < 1 || y >= Main.maxTilesY - 2) return true;
		if (band.Protected(x, y)) return true;
		Tile tile = Main.tile[x, y];
		if (tile.HasTile && (ContinentalNaturalTiles.IsDungeonBrick(tile.TileType) || tile.TileType == TileID.LihzahrdBrick))
			return true;
		return tile.WallType is WallID.LihzahrdBrickUnsafe or WallID.BlueDungeonUnsafe or WallID.GreenDungeonUnsafe or WallID.PinkDungeonUnsafe;
	}

	/// <summary>
	/// Called before the carve writes cell (x, y). Plain cells pass straight through.
	/// A frame-important object is resolved to its TileObjectData footprint: if every
	/// footprint cell is unprotected the whole object goes (its chest record and tile
	/// entities too) and true is returned; if any cell is protected the object stays
	/// whole and false is returned, so the caller must leave this cell alone.
	/// </summary>
	private static bool ReleaseCell(ProtectionBand band, ObjectLedger ledger, int x, int y)
	{
		Tile tile = Main.tile[x, y];
		if (!tile.HasTile || !Main.tileFrameImportant[tile.TileType]) return true;
		ushort type = tile.TileType;
		int left = x, top = y, width = 1, height = 1;
		TileObjectData data = TileObjectData.GetTileData(tile);
		if (data != null)
		{
			if (data.Width > MaxObjectSide || data.Height > MaxObjectSide) { ledger.Kept++; return false; }
			Point16 origin = TileObjectData.TopLeft(x, y);
			if (origin.X >= 0 && origin.Y >= 0 && x >= origin.X && x < origin.X + data.Width && y >= origin.Y && y < origin.Y + data.Height)
			{
				left = origin.X; top = origin.Y; width = data.Width; height = data.Height;
			}
		}
		for (int cx = left; cx < left + width; cx++)
		for (int cy = top; cy < top + height; cy++)
			if (CellProtected(band, cx, cy)) { ledger.Kept++; return false; }

		bool container = Main.tileContainer[type] || TileID.Sets.BasicChest[type];
		for (int cx = left; cx < left + width; cx++)
		for (int cy = top; cy < top + height; cy++)
		{
			if (container)
			{
				int chest = Chest.FindChest(cx, cy);
				if (chest >= 0) { Chest.DestroyChestDirect(cx, cy, chest); ledger.Chests++; }
			}
			var position = new Point16(cx, cy);
			if (TileEntity.ByPosition.TryGetValue(position, out TileEntity entity))
			{
				if (entity is ModTileEntity modEntity) modEntity.Kill(cx, cy);
				if (TileEntity.ByPosition.TryGetValue(position, out TileEntity remaining) && remaining == entity)
				{
					TileEntity.ByID.Remove(entity.ID);
					TileEntity.ByPosition.Remove(position);
				}
				ledger.Entities++;
			}
		}
		for (int cx = left; cx < left + width; cx++)
		for (int cy = top; cy < top + height; cy++)
		{
			Tile part = Main.tile[cx, cy];
			if (!part.HasTile || part.TileType != type) continue; // not this object's cell: never touched
			part.ClearTile();
		}
		// The footprint may reach past the envelope: re-frame its ring locally (protected cells are skipped).
		ledger.Framed += band.Frame(left - 1, top - 1, left + width, top + height);
		ledger.Cleared++;
		return true;
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

	private static (int Cells, int Left, int Right, int Skipped, int Framed) CarveMetro(int floorY, Rectangle[] reservations, ObjectLedger objects)
	{
		int center = Main.maxTilesX / 2;
		int cells = 0, skipped = 0;
		var band = new ProtectionBand(new Rectangle(0, floorY - MetroHeight - 2, Main.maxTilesX, MetroHeight + 6), reservations);
		int left = Sweep(band, objects, center, -1, floorY, ref cells, ref skipped);
		int right = Sweep(band, objects, center + 1, +1, floorY, ref cells, ref skipped);
		int framed = band.Frame(left - 1, floorY - MetroHeight - 1, right + 1, floorY + 2);
		return (cells, left, right, skipped, framed);
	}

	// Walks from the world centre towards one edge. Returns the last column reached.
	private static int Sweep(ProtectionBand band, ObjectLedger objects, int start, int step, int floorY, ref int cells, ref int skipped)
	{
		int liquidRun = 0, last = start;
		// V03 29.10: the metro ran to the map edge through Calamity's Abyss caves on one side and,
		// with the cave ocean, through the flooded rock on the other: a water tunnel into the whole
		// continent. Both sea bands (the Ding Dong ocean width on either side) end the line.
		int seaBand = OceanExpeditions.OceanExpeditionLayout.WidthFor(Main.maxTilesX);
		for (int x = start; x >= Math.Max(MetroEdgeMargin, seaBand) && x < Main.maxTilesX - Math.Max(MetroEdgeMargin, seaBand); x += step)
		{
			// S3 run: underground lakes ended the line mid-map (7744..17267 of 28000).
			// Only a column that is water from the open sky down to the track row is ocean.
			if (OceanColumn(x, floorY))
			{
				if (++liquidRun >= OceanStopColumns) break;
			}
			else liquidRun = 0;
			if (ColumnProtected(band, x, floorY - MetroHeight, MetroHeight + 2)) { skipped++; last = x; continue; }
			// An object on the rail cell that reaches into a protected cell keeps the whole column untouched.
			if (!ReleaseCell(band, objects, x, floorY)) { skipped++; last = x; continue; }
			for (int y = floorY - MetroHeight; y < floorY; y++)
			{
				if (!ReleaseCell(band, objects, x, y)) continue; // kept object: its cells stay as they are
				Tile t = Main.tile[x, y];
				t.ClearEverything();
			}
			Tile rail = Main.tile[x, floorY];
			rail.ClearEverything();
			rail.HasTile = true;
			rail.TileType = TileID.MinecartTrack;
			Tile bed = Main.tile[x, floorY + 1];
			if ((!bed.HasTile || !Main.tileSolid[bed.TileType]) && ReleaseCell(band, objects, x, floorY + 1))
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

	private static (int Top, int Bottom, int Cells, int Skipped, int LavaCleared, int Framed, int TrackKept, int BedPlatforms) CarveShaft(int x, int metroFloorY, int groundHint, Rectangle[] reservations, ObjectLedger objects)
	{
		int top = SurfaceAt(x, groundHint);
		int bottom = Main.maxTilesY - 200 - ShaftBottomAboveUnderworld; // vanilla underworld starts at maxTilesY-200
		int cells = 0, skipped = 0, lava = 0, trackKept = 0, bedPlatforms = 0;
		var band = new ProtectionBand(new Rectangle(x - ShaftHalfWidth - 3, 0, 2 * ShaftHalfWidth + 7, Main.maxTilesY), reservations);
		for (int y = top; y < bottom; y++)
		{
			// The obsidian edge cells (x +- 2) are written too, so the whole 5-wide strip must be free.
			bool blocked = false;
			for (int dx = -ShaftHalfWidth - 1; dx <= ShaftHalfWidth + 1 && !blocked; dx++) blocked = ColumnProtected(band, x + dx, y, 1);
			if (blocked) { skipped++; continue; }
			bool centerOpen = false;
			for (int dx = -ShaftHalfWidth; dx <= ShaftHalfWidth; dx++)
			{
				Tile t = Main.tile[x + dx, y];
				bool lavaCell = t.LiquidAmount > 0 && t.LiquidType == LiquidID.Lava;
				// S5 review F5: the metro is carved first; its rail stays across the shaft
				// (minecart track is not solid, so a falling player still passes it).
				if (y == metroFloorY && t.HasTile && t.TileType == TileID.MinecartTrack)
				{
					if (lavaCell) lava++;
					t.LiquidAmount = 0;
					trackKept++;
					continue;
				}
				if (!ReleaseCell(band, objects, x + dx, y)) continue; // kept object: its cells stay as they are
				if (lavaCell) lava++;
				t.ClearEverything();
				cells++;
				Tile above = Main.tile[x + dx, y - 1];
				if (y == metroFloorY + 1 && above.HasTile && above.TileType == TileID.MinecartTrack)
				{
					// The rail's bed across the shaft is a platform: it carries the line and
					// catches a falling player at the station, who drops through it (Down)
					// to the rope below. A brick bed would plug the hellevator.
					t.HasTile = true;
					t.TileType = TileID.Platforms;
					t.TileFrameX = 0;
					t.TileFrameY = 0;
					bedPlatforms++;
					continue;
				}
				if (dx == 0) centerOpen = true;
			}
			// Lava pockets beside the shaft would pour in; turn their edge cells to obsidian.
			for (int dx = -ShaftHalfWidth - 1; dx <= ShaftHalfWidth + 1; dx += 2 * ShaftHalfWidth + 2)
			{
				Tile edge = Main.tile[x + dx, y];
				if (edge.LiquidAmount > 0 && edge.LiquidType == LiquidID.Lava && ReleaseCell(band, objects, x + dx, y))
				{
					edge.ClearEverything();
					edge.HasTile = true;
					edge.TileType = TileID.Obsidian;
					lava++;
				}
			}
			if (y != metroFloorY && centerOpen)
			{
				Tile rope = Main.tile[x, y];
				rope.HasTile = true;
				rope.TileType = TileID.Rope;
			}
		}
		int framed = band.Frame(x - ShaftHalfWidth - 2, top - 1, x + ShaftHalfWidth + 2, bottom + 1);
		return (top, bottom, cells, skipped, lava, framed, trackKept, bedPlatforms);
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
