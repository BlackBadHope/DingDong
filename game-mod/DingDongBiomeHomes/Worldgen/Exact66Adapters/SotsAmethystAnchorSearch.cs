using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class SotsAmethystAnchorSearch
{
	// -1: pyramid; 0: no relevant tile; positive: first sand Y.
	// Column reads are pure: cache once, preserving the original 61-column scoring,
	// its -1 early exit and first-wins tie ordering. No RNG calls are removed.
	internal static bool TryFind(int left, int right, Func<int, int> firstEvent,
		out int anchorX, out int anchorY)
	{
		anchorX = anchorY = 0;
		int width = right - left;
		if (width < 61) return false;
		int[] columns = new int[width];
		for (int x = 0; x < width; x++) columns[x] = firstEvent(left + x);
		int best = 0;
		for (int start = 0; start < width - 60; start++)
		{
			int score = 0, top = 0;
			for (int j = 0; j <= 60; j++)
			{
				int y = columns[start + j];
				if (y < 0) score = -1;
				else if (y > 0)
				{
					if (top == 0) top = y;
					score += Math.Abs(y - top) <= 2 ? 1 : -1;
				}
				if (score == -1) break;
			}
			if (score <= best) continue;
			best = score;
			(anchorX, anchorY) = (left + start + 30, top);
		}
		return best > 0;
	}
}
