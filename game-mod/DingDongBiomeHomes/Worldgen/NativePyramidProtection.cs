using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// SOTS does not publish this progression structure to StructureMap. Register its
// actual occupied envelope before our planner can admit a destructive residence.
internal sealed partial class NativePyramidProtection : ModSystem
{
	private static readonly List<Cell> cells = new();
	private static Rectangle area;
	private static bool captured;
	private static GuardDescription description;
	private readonly record struct Cell(int X, int Y, bool Solid, ushort Tile, ushort Wall);
	internal sealed record GuardDescription(Rectangle LegacyEnvelope, int SourceCellCount,
		int GateCellCount, Rectangle GateBounds, IReadOnlyList<Rectangle> Regions, int ExtraSourceCells = 0)
	{
		internal int TotalProtectedSourceCells => SourceCellCount + ExtraSourceCells;
	}
	internal static object SnapshotProvenance => description == null ? null : new
	{
		owner = "DingDongBiomeHomes/NativePyramidProtection", shapeVersion = "filled-row-hull-v1",
		legacyEnvelope = Coordinates(description.LegacyEnvelope),
		gateBounds = Coordinates(description.GateBounds),
		sourceCellCount = description.SourceCellCount, gateCellCount = description.GateCellCount,
		extraSourceCells = description.ExtraSourceCells,
		totalProtectedSourceCells = description.TotalProtectedSourceCells,
		uncoveredProtectedSourceCells = 0,
		regions = description.Regions.Select(Coordinates).ToArray()
	};

	public override void ClearWorld() => Reset();
	public override void PreWorldGen() => Reset();
	public override void Unload() => Reset();
	// RamOpt 28.10: TrimExcess so the ~1.5 MB x10 cell list is not kept for the whole session.
	private static void Reset() { cells.Clear(); cells.TrimExcess(); captured = false; area = default; description = null; }

	// RamOpt 28.10: the post-worldgen check is the last reader of the generation-time copy.
	internal static void ReleaseAfterWorldgen() => Reset();

	internal static void Reserve(Mod owner)
	{
		if (Compiler.Runtime.WorldCompilerActivation.IsActive || captured ||
			!ModLoader.TryGetMod("SOTS", out Mod sots)) return;
		var field = sots.Code.GetType("SOTS.WorldgenHelpers.PyramidWorldgenHelper")?
			.GetField("placementLocation", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
		if (field?.GetValue(null) is not Vector2 origin || origin.X <= 0 || origin.Y <= 0)
			throw new InvalidOperationException("Cannot protect SOTS pyramid: missing placement origin.");
		Rectangle search = Rectangle.Intersect(new Rectangle((int)origin.X - 640,
			(int)origin.Y - 64, 1281, 641), WorldBounds);
		description = Scan(sots, search, out List<Cell> sourceCells);
		cells.AddRange(sourceCells);
		area = description.LegacyEnvelope;
		// Append only our new guard. Never remove or reinterpret provider entries.
		foreach (Rectangle region in description.Regions)
			GenVars.structures.AddProtectedStructure(region, 0);
		captured = true;
		owner.Logger.Info($"DINGDONG_PYRAMID|state=RESERVED|area={area}|cells={description.SourceCellCount}|extraSourceCells={description.ExtraSourceCells}|protectedSourceCells={cells.Count}|uncoveredSourceCells=0|gateCells={description.GateCellCount}|guardRegions={description.Regions.Count}|guardShape=filled-row-hull-v1|curseUnchanged=True");
	}

	// Caller must establish ownership of a legacy guard from provenance first.
	// This method only reads tiles and constructs geometry; it cannot migrate a
	// StructureMap or erase a foreign entry, even when rectangle shapes coincide.
	internal static GuardDescription DescribeFromVerifiedEnvelope(Rectangle verifiedLegacyEnvelope)
	{
		if (!ModLoader.TryGetMod("SOTS", out Mod sots))
			throw new InvalidOperationException("Cannot describe SOTS pyramid without SOTS.");
		if (Rectangle.Intersect(verifiedLegacyEnvelope, WorldBounds) != verifiedLegacyEnvelope)
			throw new InvalidOperationException("Verified pyramid envelope extends outside the world.");
		GuardDescription result = Scan(sots, verifiedLegacyEnvelope, out _);
		if (result.LegacyEnvelope != verifiedLegacyEnvelope)
			throw new InvalidOperationException("Current pyramid does not reproduce the verified legacy envelope.");
		return result;
	}

	private static Rectangle WorldBounds => new(1, 1, Main.maxTilesX - 2, Main.maxTilesY - 2);
	private static object Coordinates(Rectangle r) => new { r.X, r.Y, r.Width, r.Height };

	private static GuardDescription Scan(Mod sots, Rectangle search, out List<Cell> sourceCells)
	{
		if (!sots.TryFind<ModTile>("PyramidGateTile", out var gate))
			throw new InvalidOperationException("Cannot protect SOTS pyramid: unknown gate type.");
		int scannedGateType = gate.Type;
		sourceCells = new();
		bool[] tiles = new bool[TileLoader.TileCount], walls = new bool[WallLoader.WallCount];
		foreach (var tile in sots.GetContent<ModTile>())
			if (PyramidName(tile.Name)) tiles[tile.Type] = true;
		foreach (var wall in sots.GetContent<ModWall>())
			if (PyramidName(wall.Name)) walls[wall.Type] = true;
		int left = search.Right, right = search.Left, top = search.Bottom, bottom = search.Top, gates = 0;
		int gateLeft = search.Right, gateRight = search.Left, gateTop = search.Bottom, gateBottom = search.Top;
		for (int x = search.Left; x < search.Right; x++)
		for (int y = search.Top; y < search.Bottom; y++)
		{
			Tile t = Main.tile[x, y];
			if (!(t.HasTile && tiles[t.TileType]) && !walls[t.WallType]) continue;
			// A clipped search is never accepted as a complete progression envelope.
			if (x == search.Left || x == search.Right - 1 || y == search.Top || y == search.Bottom - 1)
				throw new InvalidOperationException("SOTS pyramid exceeds the verified protection search.");
			sourceCells.Add(new(x, y, t.HasTile, t.TileType, t.WallType));
			left = Math.Min(left, x); right = Math.Max(right, x);
			top = Math.Min(top, y); bottom = Math.Max(bottom, y);
			if (t.HasTile && t.TileType == scannedGateType)
			{
				gates++;
				gateLeft = Math.Min(gateLeft, x); gateRight = Math.Max(gateRight, x);
				gateTop = Math.Min(gateTop, y); gateBottom = Math.Max(gateBottom, y);
			}
		}
		if (gates == 0) throw new InvalidOperationException("SOTS pyramid has no legitimate gate before residences.");
		Rectangle gateBounds = new(gateLeft, gateTop, gateRight - gateLeft + 1, gateBottom - gateTop + 1);
		TileObjectData gateData = TileObjectData.GetTileData(scannedGateType, 0);
		if (gateData == null || gateBounds.Width != gateData.Width || gateBounds.Height != gateData.Height ||
			gates != checked(gateData.Width * gateData.Height))
			throw new InvalidOperationException("SOTS pyramid gate is incomplete or its registered dimensions changed.");
		Rectangle envelope = Rectangle.Intersect(new Rectangle(left - 24, top - 32,
			right - left + 49, bottom - top + 57), WorldBounds);
		// Freeze legacy identity before adding exact writer content. A saved
		// pre-v1 envelope and its old source count remain independently provable.
		int legacySourceCellCount = sourceCells.Count;
		IncludeWriterContent(sots, envelope, tiles, walls, sourceCells);
		List<Rectangle> guard = PyramidGuardGeometry.Build(sourceCells.Select(c => (c.X, c.Y)),
			new(gateBounds.X, gateBounds.Y, gateBounds.Width, gateBounds.Height), Main.maxTilesX, Main.maxTilesY)
			.Select(r => new Rectangle(r.X, r.Y, r.Width, r.Height)).ToList();
		if (sourceCells.Any(c => !GuardContains(guard, c.X, c.Y)))
			throw new InvalidOperationException("SOTS writer content falls outside the filled pyramid guard.");
		return new(envelope, legacySourceCellCount, gates, gateBounds, guard.AsReadOnly(),
			sourceCells.Count - legacySourceCellCount);
	}

	private static void IncludeWriterContent(Mod sots, Rectangle envelope, bool[] legacyTiles,
		bool[] legacyWalls, List<Cell> sourceCells)
	{
		bool[] tiles = new bool[TileLoader.TileCount], walls = new bool[WallLoader.WallCount];
		foreach (string name in PyramidWriterContent.Tiles)
		{
			if (!sots.TryFind<ModTile>(name, out var tile))
				throw new InvalidOperationException($"Cannot verify SOTS pyramid writer tile {name}.");
			tiles[tile.Type] = true;
		}
		foreach (string name in PyramidWriterContent.Walls)
		{
			if (!sots.TryFind<ModWall>(name, out var wall))
				throw new InvalidOperationException($"Cannot verify SOTS pyramid writer wall {name}.");
			walls[wall.Type] = true;
		}
		for (int x = envelope.Left; x < envelope.Right; x++)
		for (int y = envelope.Top; y < envelope.Bottom; y++)
		{
			Tile t = Main.tile[x, y];
			if (!(t.HasTile && tiles[t.TileType]) && !walls[t.WallType]) continue;
			if (t.HasTile && legacyTiles[t.TileType] || legacyWalls[t.WallType]) continue;
			sourceCells.Add(new(x, y, t.HasTile, t.TileType, t.WallType));
		}
	}

	private static bool GuardContains(IReadOnlyList<Rectangle> regions, int x, int y)
	{
		int lo = 0, hi = regions.Count - 1;
		while (lo <= hi)
		{
			int mid = lo + (hi - lo) / 2;
			Rectangle r = regions[mid];
			if (y < r.Top) hi = mid - 1;
			else if (y >= r.Bottom) lo = mid + 1;
			else return x >= r.Left && x < r.Right;
		}
		return false;
	}

	private static bool PyramidName(string name) => name.Contains("Pyramid", StringComparison.Ordinal) ||
		name.Contains("Cursed", StringComparison.Ordinal) || name is "RoyalGoldBrickTile" or "AncientGoldSpikeTile";

	internal static void Validate(Mod owner, string phase)
	{
		if (!captured) return;
		foreach (Cell c in cells)
		{
			Tile t = Main.tile[c.X, c.Y];
			if (t.HasTile != c.Solid || c.Solid && t.TileType != c.Tile || t.WallType != c.Wall)
				throw new InvalidOperationException($"SOTS pyramid changed during {phase} at {c.X},{c.Y}; acceptance failed.");
		}
		owner.Logger.Info($"DINGDONG_PYRAMID|state=PRESERVED|phase={phase}|cells={description.SourceCellCount}|extraSourceCells={description.ExtraSourceCells}|protectedSourceCells={cells.Count}|curseUnchanged=True");
	}
}
