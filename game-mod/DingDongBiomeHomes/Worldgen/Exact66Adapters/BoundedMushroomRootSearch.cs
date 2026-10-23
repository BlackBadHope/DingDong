using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class BoundedMushroomRootSearch
{
	// Native rejection sampling cannot terminate when this finite window is empty.
	// Keep the first draw; otherwise sample uniformly among eligible cells without
	// allocations. No eligible cell means no optional root, not a missing biome.
	internal static bool TrySelect(int centerX, int centerY, int width, int height,
		Func<int, int, bool> eligible, Func<int, int> next, ref int x, ref int y)
	{
		int left = Math.Max(0, centerX - 20), right = Math.Min(width, centerX + 20);
		int top = Math.Max(0, centerY), bottom = Math.Min(height, centerY + 20);
		if (x >= left && x < right && y >= top && y < bottom && eligible(x, y))
			return true;
		int count = 0, selectedX = x, selectedY = y;
		for (int column = left; column < right; column++)
			for (int row = top; row < bottom; row++)
				if (eligible(column, row) && next(++count) == 0)
				{
					selectedX = column;
					selectedY = row;
				}
		if (count == 0) return false;
		x = selectedX;
		y = selectedY;
		return true;
	}
}
