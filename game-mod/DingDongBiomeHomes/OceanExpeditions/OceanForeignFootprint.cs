using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.OceanExpeditions;

// V03 29.10: SOTS picks its pyramid site in "SOTS: Structures" (before the open
// ocean is carved) and builds it in "SOTS: Pyramid" (after). Keep that site out of
// the predicted basin, then prove no foreign footprint entered the basin water.
internal static class OceanForeignFootprint
{
	// V03 29.10: = NativePyramidProtection search half-width around the origin.
	internal const int PyramidMargin = 640;
	private const int Block = 32;
	internal static bool Scheduled { get; set; }
	internal static Point AmberAnchor { get; set; } = new(-1, -1);
	private static BitArray baseline;
	private static Rectangle water;
	private static int baselineCells, pyramidRolls, pyramidRemaps, pyramidUnresolved;

	internal static void Arm(bool scheduled)
	{
		Scheduled = scheduled;
		AmberAnchor = new(-1, -1);
		Release();
		pyramidRolls = pyramidRemaps = pyramidUnresolved = 0;
	}

	internal static void Release() { baseline = null; water = default; baselineCells = 0; }

	// V03 29.10: same side rule as Generate(): the sea sits opposite the dungeon
	// (Calamity's Abyss is pinned to the dungeon side), width = WidthFor().
	private static bool TryBasinSpan(out int lo, out int hi)
	{
		lo = hi = 0;
		if (!Scheduled || !(WorldGen.generatingWorld || WorldGen.gen)) return false;
		OceanExpeditionLayout layout = OceanExpeditionWorldSystem.Layout;
		bool left = layout?.Left ?? Main.dungeonX >= Main.maxTilesX / 2;
		int width = layout?.Width ?? OceanExpeditionLayout.WidthFor(Main.maxTilesX);
		lo = left ? 0 : Main.maxTilesX - width;
		hi = left ? width : Main.maxTilesX;
		return true;
	}

	// V03 29.10: applied to each genRand roll of PyramidWorldgenHelper.DeterminePyramidLocation.
	// Same RNG draws; only a roll inside basin+margin is folded onto the land part of SOTS' range.
	internal static int AdmitPyramidX(int x)
	{
		if (!TryBasinSpan(out int lo, out int hi)) return x;
		pyramidRolls++;
		int fLo = Math.Max(0, lo - PyramidMargin), fHi = Math.Min(Main.maxTilesX, hi + PyramidMargin);
		if (x < fLo || x >= fHi) return x;
		bool sotsLeft = Main.dungeonX > Main.maxTilesX / 2;
		int rLo = sotsLeft ? 500 : Main.maxTilesX / 2, rHi = sotsLeft ? Main.maxTilesX / 2 : Main.maxTilesX - 500;
		int oLo = Math.Max(rLo, fLo), oHi = Math.Min(rHi, fHi);
		int aLo = lo == 0 ? Math.Max(rLo, fHi) : rLo, aHi = lo == 0 ? rHi : Math.Min(rHi, fLo);
		if (oHi <= oLo || aHi - aLo < 1 || x < oLo || x >= oHi) { pyramidUnresolved++; return x; }
		pyramidRemaps++;
		return aLo + (int)((long)(x - oLo) * (aHi - aLo) / (oHi - oLo));
	}

	private static Rectangle WaterRegion(OceanExpeditionLayout p) =>
		new(p.Bounds.X, p.SeaLevel, p.Bounds.Width, p.Bottom - p.SeaLevel + 1);

	private static (bool[] tiles, bool[] walls) ForeignTypes(Mod owner)
	{
		bool[] tiles = new bool[TileLoader.TileCount], walls = new bool[WallLoader.WallCount];
		for (int t = TileID.Count; t < tiles.Length; t++) tiles[t] = TileLoader.GetTile(t)?.Mod != owner;
		for (int w = WallID.Count; w < walls.Length; w++) walls[w] = WallLoader.GetWall(w)?.Mod != owner;
		return (tiles, walls);
	}

	private static bool Foreign(int x, int y, bool[] tiles, bool[] walls)
	{
		Tile t = Main.tile[x, y];
		return t.HasTile && tiles[t.TileType] || walls[t.WallType];
	}

	// V03 29.10: foreign cells already in the water when the sea is finished (retained provider islands).
	internal static void CaptureBaseline(OceanExpeditionLayout plan, Mod owner)
	{
		water = WaterRegion(plan);
		var (tiles, walls) = ForeignTypes(owner);
		baseline = new BitArray(checked(water.Width * water.Height));
		baselineCells = 0;
		for (int x = water.Left; x < water.Right; x++)
		for (int y = water.Top; y < water.Bottom; y++)
			if (Foreign(x, y, tiles, walls)) { baseline[(x - water.Left) * water.Height + y - water.Top] = true; baselineCells++; }
	}

	internal static void Receipt(Mod owner)
	{
		if (OceanExpeditionWorldSystem.Layout == null || baseline == null)
		{
			owner.Logger.Warn("DINGDONG_OCEAN_FOREIGN_FOOTPRINT|verdict=SKIPPED|reason=no-basin-baseline");
			return;
		}
		var (tiles, walls) = ForeignTypes(owner);
		int bw = (water.Width + Block - 1) / Block, bh = (water.Height + Block - 1) / Block;
		int[] count = new int[bw * bh];
		string[] owners = new string[bw * bh];
		int added = 0, lost = 0;
		for (int x = water.Left; x < water.Right; x++)
		for (int y = water.Top; y < water.Bottom; y++)
		{
			bool before = baseline[(x - water.Left) * water.Height + y - water.Top];
			bool now = Foreign(x, y, tiles, walls);
			if (before && !now) lost++;
			if (!now || before) continue;
			added++;
			int b = (x - water.Left) / Block * bh + (y - water.Top) / Block;
			count[b]++;
			owners[b] ??= OwnerName(x, y, tiles);
		}
		var intrusions = new List<string>();
		string intended = "none";
		bool[] seen = new bool[count.Length];
		var queue = new Queue<int>();
		for (int start = 0; start < count.Length; start++)
		{
			if (count[start] == 0 || seen[start]) continue;
			int cells = 0, minBx = int.MaxValue, minBy = int.MaxValue, maxBx = -1, maxBy = -1;
			string mod = owners[start];
			seen[start] = true; queue.Enqueue(start);
			while (queue.Count > 0)
			{
				int b = queue.Dequeue(), bx = b / bh, by = b % bh;
				cells += count[b];
				minBx = Math.Min(minBx, bx); maxBx = Math.Max(maxBx, bx);
				minBy = Math.Min(minBy, by); maxBy = Math.Max(maxBy, by);
				for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
				{
					int nx = bx + dx, ny = by + dy;
					if (nx < 0 || ny < 0 || nx >= bw || ny >= bh) continue;
					int n = nx * bh + ny;
					if (count[n] == 0 || seen[n]) continue;
					seen[n] = true; queue.Enqueue(n);
				}
			}
			Rectangle area = new(water.Left + minBx * Block, water.Top + minBy * Block,
				(maxBx - minBx + 1) * Block, (maxBy - minBy + 1) * Block);
			string entry = $"{mod}@{area.X},{area.Y},{area.Width},{area.Height}:{cells}";
			// V03 29.10: SOTS' Amber water vault is authored for sea water (anchor = water over sand).
			Rectangle amberZone = area; amberZone.Inflate(Block, Block);
			if (mod == "SOTS" && AmberAnchor.X >= 0 && amberZone.Contains(AmberAnchor)) intended = "amberVault:" + entry;
			else intrusions.Add(entry);
		}
		Vector2 pyramid = ReadStatic<Vector2>("SOTS.WorldgenHelpers.PyramidWorldgenHelper", "placementLocation");
		Rectangle village = ReadStatic<Rectangle>("SOTS.WorldgenHelpers.AbandonedVillageWorldgenHelper", "OuterRect");
		TryBasinSpan(out int lo, out int hi);
		bool pyramidIn = pyramid.X > 0 && pyramid.X >= lo - PyramidMargin && pyramid.X < hi + PyramidMargin;
		bool villageIn = village.Width > 0 && village.Intersects(water);
		bool pass = intrusions.Count == 0 && !pyramidIn && !villageIn && pyramidUnresolved == 0;
		string line = $"DINGDONG_OCEAN_FOREIGN_FOOTPRINT|verdict={(pass ? "PASS" : "FAIL")}" +
			$"|water={water.X},{water.Y},{water.Width},{water.Height}|baselineForeignCells={baselineCells}" +
			$"|newForeignCells={added}|lostForeignCells={lost}|intrusions={intrusions.Count}|intended={intended}" +
			$"|pyramidOrigin={(int)pyramid.X},{(int)pyramid.Y}|pyramidInBasin={pyramidIn}|pyramidRolls={pyramidRolls}" +
			$"|pyramidRemaps={pyramidRemaps}|pyramidUnresolved={pyramidUnresolved}" +
			$"|villageOuter={village.X},{village.Y},{village.Width},{village.Height}|villageInBasin={villageIn}" +
			$"|first={(intrusions.Count == 0 ? "none" : string.Join(";", intrusions.Take(4)))}";
		if (pass) owner.Logger.Info(line); else owner.Logger.Error(line);
		Release();
	}

	private static string OwnerName(int x, int y, bool[] tiles)
	{
		Tile t = Main.tile[x, y];
		return (t.HasTile && tiles[t.TileType] ? TileLoader.GetTile(t.TileType)?.Mod?.Name
			: WallLoader.GetWall(t.WallType)?.Mod?.Name) ?? "unknown";
	}

	private static T ReadStatic<T>(string type, string field) where T : struct
	{
		if (!ModLoader.TryGetMod("SOTS", out Mod sots)) return default;
		return sots.Code?.GetType(type)?.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			?.GetValue(null) is T value ? value : default;
	}
}
