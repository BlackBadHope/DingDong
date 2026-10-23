using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class SurfaceEntranceSearch
{
	// Deterministic exhaustive fallback only after the provider's random search failed.
	internal static bool Find(int left, int top, int right, int bottom, int center,
		Func<int, int, bool> eligible, out int x, out int y)
	{
		x = y = 0;
		if (left >= right || top >= bottom) return false;
		center = Math.Clamp(center, left, right - 1);
		for (int distance = 0; distance < right - left; distance++)
		{
			int a = center - distance, b = center + distance;
			if (a >= left && Scan(a, top, bottom, eligible, out y)) { x = a; return true; }
			if (distance != 0 && b < right && Scan(b, top, bottom, eligible, out y)) { x = b; return true; }
		}
		y = 0;
		return false;
	}

	private static bool Scan(int x, int top, int bottom, Func<int, int, bool> eligible, out int y)
	{
		for (y = top; y < bottom; y++) if (eligible(x, y)) return true;
		return false;
	}
}
