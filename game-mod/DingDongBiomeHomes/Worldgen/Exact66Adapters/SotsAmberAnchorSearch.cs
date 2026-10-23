using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

// Keep SOTS' water-over-sand rule, but use actual depth and valid vault anchors.
internal static class SotsAmberAnchorSearch
{
	internal static bool TryFind(int width, int bottom, bool right,
		Func<int, int, bool> sand, Func<int, int, bool> water,
		out int anchorX, out int anchorY)
	{
		anchorX = anchorY = 0;
		if (width < 800 || bottom <= 50) return false;
		int step = right ? -1 : 1;
		int first = right ? width - 51 : 50;
		int last = right ? width - 400 : 400;
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
