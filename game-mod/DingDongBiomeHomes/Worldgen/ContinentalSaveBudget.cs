using System;
using System.Numerics;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen;

internal readonly struct ContinentalSaveBudgetReceipt
{
	internal ContinentalSaveBudgetReceipt(long cells, long modTiles,
		long frameImportantModTiles, long modWalls, long tileDataBytes,
		long wallDataBytes, int scanWorkers, long marbleCells,
		long graniteCells, long minecartTrackCells, long livingTreeCells,
		long trapCells, int marbleSectors, int graniteSectors,
		int minecartTrackSectors, int livingTreeSectors, int trapSectors)
	{
		Cells = cells;
		ModTiles = modTiles;
		FrameImportantModTiles = frameImportantModTiles;
		ModWalls = modWalls;
		TileDataBytes = tileDataBytes;
		WallDataBytes = wallDataBytes;
		ScanWorkers = scanWorkers;
		MarbleCells = marbleCells;
		GraniteCells = graniteCells;
		MinecartTrackCells = minecartTrackCells;
		LivingTreeCells = livingTreeCells;
		TrapCells = trapCells;
		MarbleSectors = marbleSectors;
		GraniteSectors = graniteSectors;
		MinecartTrackSectors = minecartTrackSectors;
		LivingTreeSectors = livingTreeSectors;
		TrapSectors = trapSectors;
	}

	internal long Cells { get; }
	internal long ModTiles { get; }
	internal long FrameImportantModTiles { get; }
	internal long ModWalls { get; }
	internal long TileDataBytes { get; }
	internal long WallDataBytes { get; }
	internal long RawBytes => checked(TileDataBytes + WallDataBytes);
	internal int ScanWorkers { get; }
	internal long MarbleCells { get; }
	internal long GraniteCells { get; }
	internal long MinecartTrackCells { get; }
	internal long LivingTreeCells { get; }
	internal long TrapCells { get; }
	internal int MarbleSectors { get; }
	internal int GraniteSectors { get; }
	internal int MinecartTrackSectors { get; }
	internal int LivingTreeSectors { get; }
	internal int TrapSectors { get; }
	internal string PoiCompact() =>
		$"marble={MarbleCells}/{MarbleSectors},granite={GraniteCells}/{GraniteSectors}," +
		$"tracks={MinecartTrackCells}/{MinecartTrackSectors}," +
		$"livingTree={LivingTreeCells}/{LivingTreeSectors},traps={TrapCells}/{TrapSectors}";
}

internal static class ContinentalSaveBudget
{
	internal const long RawPlaneCapBytes = 1_879_048_135L;
	private static ContinentalSaveBudgetReceipt? cached;
	private static int scans;

	internal static int ScanCount => scans;

	internal static void Reset()
	{
		cached = null;
		scans = 0;
	}

	internal static ContinentalSaveBudgetReceipt MeasureOnce()
	{
		if (cached.HasValue)
			return cached.Value;
		if (scans != 0)
			throw new InvalidOperationException("Continental save-budget scan re-entry.");
		scans = 1;
		long cells = checked((long)Main.maxTilesX * Main.maxTilesY);
		var totals = new ScanTotals();
		object mergeLock = new();
		int workers = Math.Clamp(Environment.ProcessorCount, 1, 8);
		var options = new ParallelOptions { MaxDegreeOfParallelism = workers };
		Parallel.For(0, Main.maxTilesX, options, () => new ScanTotals(),
			(x, _, local) =>
			{
				int sector = 1 << Math.Clamp(x * 8 / Main.maxTilesX, 0, 7);
				for (int y = 0; y < Main.maxTilesY; y++)
				{
					Tile tile = Main.tile[x, y];
					if (tile.HasTile)
						switch (tile.TileType)
						{
							case TileID.MarbleBlock:
								local.MarbleCells++;
								local.MarbleSectors |= sector;
								break;
							case TileID.GraniteBlock:
								local.GraniteCells++;
								local.GraniteSectors |= sector;
								break;
							case TileID.MinecartTrack:
								local.MinecartTrackCells++;
								local.MinecartTrackSectors |= sector;
								break;
							case TileID.LivingWood:
							case TileID.LeafBlock:
								local.LivingTreeCells++;
								local.LivingTreeSectors |= sector;
								break;
							case TileID.Traps:
								local.TrapCells++;
								local.TrapSectors |= sector;
								break;
						}
					if (tile.HasTile && tile.TileType >= TileID.Count)
					{
						local.ModTiles++;
						if (tile.TileType >= Main.tileFrameImportant.Length)
							local.InvalidTileType = tile.TileType;
						else if (Main.tileFrameImportant[tile.TileType])
							local.Framed++;
					}
					if (tile.WallType >= WallID.Count)
						local.ModWalls++;
				}
				return local;
			},
			local =>
			{
				lock (mergeLock)
					totals.Merge(local);
			});
		if (totals.InvalidTileType >= 0)
			throw new InvalidOperationException(
				$"Invalid mod tile type {totals.InvalidTileType}.");
		long tileDataBytes = checked(2L * cells + totals.ModTiles +
			4L * totals.Framed);
		long wallDataBytes = checked(2L * cells + totals.ModWalls);
		cached = new ContinentalSaveBudgetReceipt(cells, totals.ModTiles,
			totals.Framed, totals.ModWalls, tileDataBytes, wallDataBytes,
			workers, totals.MarbleCells, totals.GraniteCells,
			totals.MinecartTrackCells, totals.LivingTreeCells, totals.TrapCells,
			BitOperations.PopCount((uint)totals.MarbleSectors),
			BitOperations.PopCount((uint)totals.GraniteSectors),
			BitOperations.PopCount((uint)totals.MinecartTrackSectors),
			BitOperations.PopCount((uint)totals.LivingTreeSectors),
			BitOperations.PopCount((uint)totals.TrapSectors));
		return cached.Value;
	}

	private sealed class ScanTotals
	{
		internal long ModTiles;
		internal long Framed;
		internal long ModWalls;
		internal int InvalidTileType = -1;
		internal long MarbleCells;
		internal long GraniteCells;
		internal long MinecartTrackCells;
		internal long LivingTreeCells;
		internal long TrapCells;
		internal int MarbleSectors;
		internal int GraniteSectors;
		internal int MinecartTrackSectors;
		internal int LivingTreeSectors;
		internal int TrapSectors;

		internal void Merge(ScanTotals other)
		{
			ModTiles += other.ModTiles;
			Framed += other.Framed;
			ModWalls += other.ModWalls;
			MarbleCells += other.MarbleCells;
			GraniteCells += other.GraniteCells;
			MinecartTrackCells += other.MinecartTrackCells;
			LivingTreeCells += other.LivingTreeCells;
			TrapCells += other.TrapCells;
			MarbleSectors |= other.MarbleSectors;
			GraniteSectors |= other.GraniteSectors;
			MinecartTrackSectors |= other.MinecartTrackSectors;
			LivingTreeSectors |= other.LivingTreeSectors;
			TrapSectors |= other.TrapSectors;
			if (InvalidTileType < 0 && other.InvalidTileType >= 0)
				InvalidTileType = other.InvalidTileType;
		}
	}
}
