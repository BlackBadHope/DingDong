using System;

namespace DingDongBiomeHomes.Fishing.Worldgen;

// V03 28.10: the single route shape shared by FinishAbyss (carve) and
// AbyssWaystations (decor), so the two can no longer drift apart. The old
// shape - two parallel 88-tile shafts plus a straight 46-row band every 420
// rows across the whole 890-tile footprint - drew a blue lattice on the
// owner's map (15.webp). Now one serpentine descent: shafts wander, breathe
// and alternate sides between floors; crossings are switchbacks with a
// meandering bed, ragged roof, a vault over the middle station, ragged ends
// and a jittered pitch. The bed is level only under the three waystation
// sites so slabs still rest on it.
// Pure functions of (edge, y, top): deterministic, no genRand draw.
internal static class AbyssRouteGeometry
{
	public const int Pitch = 420, CrossingHeight = 46, EdgeFrom = 70, EdgeTo = 960, BottomMargin = 70;
	public const int StationHalf = 10, StationGap = 14, MiddleSite = 500;
	private const int MaxMeander = 34, MaxTilt = 56, MaxRoof = 15 + 24, ShaftSump = 40, SiteFlat = StationHalf + 4, SiteBlend = 40;

	public static int ShaftOffset(int y) => (int)(Math.Sin(y * 0.006) * 30 + Math.Sin(y * 0.0173 + 1.3) * 12);
	// Half-bore of shaft 0 (edge ~180) and shaft 1 (edge ~820): 31..57 instead of a constant 44/48.
	public static int ShaftHalf(int y, int shaft) => (shaft == 0 ? 44 : 48) +
		(int)(Math.Sin(y * 0.029 + shaft * 2.1) * 9 + Math.Sin(y * 0.071 + shaft) * 4);
	// Serpentine, not a ladder: above crossing 0 both shafts open to the sea; below
	// crossing j only the shaft at its low end continues (right for even j, left
	// for odd j) and ends in a short sump under crossing j + 1.
	public static bool InShaft(int edge, int y, int top, int count)
	{
		int o = ShaftOffset(y), h0 = ShaftHalf(y, 0), h1 = ShaftHalf(y, 1);
		int d0 = Math.Abs(edge - (180 + o)), d1 = Math.Abs(edge - (820 - o));
		bool near0 = d0 < h0, near1 = d1 < h1;
		if (!near0 && !near1) return false;
		// Domed top and bowl-shaped sump instead of square-cut shaft ends.
		double r = near1 ? (double)d1 / h1 : (double)d0 / h0;
		int round = (int)(r * r * 24);
		if (count <= 0 || y <= LevelFloor(top, 0) + ShaftSump - round) return true;
		int j = Math.Clamp((y - top) / Pitch, 0, count - 1);
		for (int i = Math.Max(0, j - 1); i <= j + 1 && i < count; i++)
		{
			bool right = i % 2 == 0;
			if (!(right ? near1 : near0)) continue;
			double ri = right ? (double)d1 / h1 : (double)d0 / h0;
			int bend = (int)(ri * ri * 24);
			int from = LevelFloor(top, i) - CrossingHeight + bend;
			int to = i + 1 < count ? LevelFloor(top, i + 1) + (int)Tilt(i + 1, right ? 820 : 180) + ShaftSump - bend : int.MaxValue;
			if (y >= from && y <= to) return true;
		}
		return false;
	}

	public static int CrossingCount(int top, int routeBottom)
	{
		int n = 0;
		while (LevelFloor(top, n) + MaxMeander + MaxTilt < routeBottom) n++;
		return n;
	}
	// Ceiling row of crossing k before meander; the pitch is jittered +-40 rows.
	private static int Base(int top, int k) => top + Pitch * k + (k == 0 ? 0 : (int)(Math.Sin(k * 2.39) * 40));
	// Floor row at the middle site (edge 500).
	private static int LevelFloor(int top, int k) => Base(top, k) + CrossingHeight - 1;
	// Alternate crossings descend left-to-right and right-to-left (switchbacks, not rungs).
	private static double Tilt(int k, int edge) => (k % 2 == 0 ? 0.12 : -0.12) * (edge - MiddleSite);

	// Station slab centres: just outside each shaft at the slab's own floor, plus the middle.
	public static int Site(int top, int k, int index)
	{
		if (index == 1) return MiddleSite;
		// Floor() asks for the sites per cell during the carve; memoise per (top, k).
		int slot = k * 2 + index / 2;
		if (top != siteTop || slot >= siteCache.Length)
		{
			if (top != siteTop) { siteTop = top; siteCache = new int[64]; Array.Fill(siteCache, int.MinValue); }
			if (slot >= siteCache.Length) { int old = siteCache.Length; Array.Resize(ref siteCache, slot * 2 + 2); Array.Fill(siteCache, int.MinValue, old, siteCache.Length - old); }
		}
		if (siteCache[slot] != int.MinValue) return siteCache[slot];
		int edge = index == 0 ? 180 : 820;
		for (int pass = 0; pass < 2; pass++)
		{
			int floor = LevelFloor(top, k) + (int)Tilt(k, edge), o = ShaftOffset(floor);
			edge = index == 0 ? 180 + o + ShaftHalf(floor, 0) + StationGap : 820 - o - ShaftHalf(floor, 1) - StationGap;
		}
		return siteCache[slot] = edge;
	}
	private static int siteTop = int.MinValue;
	private static int[] siteCache = Array.Empty<int>();
	// The level floor row under station site `index` (its slab rests on floor + 1).
	public static int SiteFloor(int top, int k, int index) => LevelFloor(top, k) + (int)Tilt(k, Site(top, k, index));

	public static int Floor(int top, int k, int edge)
	{
		int d = int.MaxValue, nearest = MiddleSite;
		for (int i = 0; i < 3; i++)
		{
			int s = Site(top, k, i);
			if (Math.Abs(edge - s) < d) { d = Math.Abs(edge - s); nearest = s; }
		}
		double w = Math.Clamp((d - SiteFlat) / (double)SiteBlend, 0, 1);
		double m = Math.Sin(edge * 0.0105 + k * 1.7) * 26 + Math.Sin(edge * 0.027 + k * 0.6) * 8;
		return LevelFloor(top, k) + (int)((1 - w) * (int)Tilt(k, nearest) + w * (Tilt(k, edge) + m));
	}
	// Roof above a given floor row: 31..61 rows of headroom, plus a vault over the middle station.
	public static int Ceiling(int k, int edge, int floor)
	{
		double vault = (edge - MiddleSite) / 70.0;
		return floor - (CrossingHeight - 1) - (int)(Math.Sin(edge * 0.019 + k * 2.3) * 10 +
			Math.Sin(edge * 0.047 + k * 0.9) * 5 + 24 * Math.Exp(-vault * vault));
	}

	private static bool InCrossing(int edge, int y, int top, int k)
	{
		if (k < 0) return false;
		int from = EdgeFrom + (int)((Math.Sin(k * 1.31) + 1) * 30), to = EdgeTo - (int)((Math.Sin(k * 0.77 + 2) + 1) * 30);
		if (edge < from || edge >= to) return false;
		int level = LevelFloor(top, k);
		if (y > level + MaxMeander + MaxTilt || y < level - CrossingHeight - MaxMeander - MaxTilt - MaxRoof) return false;
		int floor = Floor(top, k, edge);
		return y <= floor && y >= Ceiling(k, edge, floor);
	}

	// count = CrossingCount(top, routeBottom), hoisted out of per-cell loops by the callers.
	public static bool InRoute(int edge, int y, int top, int routeBottom, int count)
	{
		if (edge < EdgeFrom || edge >= EdgeTo || y < top || y >= routeBottom) return false;
		if (InShaft(edge, y, top, count)) return true;
		int k = (y - top) / Pitch;
		for (int j = k - 1; j <= k + 1; j++)
			if (j < count && InCrossing(edge, y, top, j)) return true;
		return false;
	}
}
