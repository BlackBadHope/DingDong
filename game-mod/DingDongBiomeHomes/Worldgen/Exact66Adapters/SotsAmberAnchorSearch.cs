using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

// Keep SOTS' water-over-sand rule, but use actual depth and valid vault anchors.
internal static class SotsAmberAnchorSearch
{
	// V03 29.10: span = coast columns to scan (SOTS: 350); the continental ocean
	// passes its whole basin width so a deep sea with a narrow edge bank still finds sand.
	internal static bool TryFind(int width, int bottom, bool right,
		Func<int, int, bool> sand, Func<int, int, bool> water,
		out int anchorX, out int anchorY, int span = 350)
	{
		anchorX = anchorY = 0;
		if (width < 800 || bottom <= 50) return false;
		span = Math.Clamp(span, 350, width / 2 - 100);
		int step = right ? -1 : 1;
		int first = right ? width - 51 : 50;
		int last = right ? width - 51 - span : 50 + span;
		for (int x = first; right ? x >= last : x <= last; x += step)
		{
			int top = -1, wet = 0, depth = 0, bestTop = -1, bestDepth = 0;
			for (int y = 0; y < bottom; y++)
			{
				if (wet > 7 && sand(x, y) && sand(x - 1, y) && sand(x + 1, y))
				{
					if (top < 0) top = y;
					depth++;
				}
				else if (depth <= 0 && water(x, y)) wet++;
				else if (wet > 7)
				{
					if (depth > 13 && top > bestTop)
						(bestTop, bestDepth) = (top, depth);
					top = -1;
					depth = wet = 0;
				}
			}
			if (bestTop >= 50 && bestTop > anchorY && bestDepth > 10)
				(anchorX, anchorY) = (x, bestTop);
		}
		return anchorY > 0;
	}
}
