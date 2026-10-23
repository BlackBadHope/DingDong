using System;
using DingDongBiomeHomes.Worldgen.Compiler;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class SotsBigGeodePlacement
{
	internal static WorldCompilerRect Footprint(int x, int y, int direction) =>
		new WorldCompilerRect(direction == 1 ? x - 104 : x, y - 50, 105, 100).Inflate(16);

	internal static (int X, int Y) Find(int x, int y, int direction, int width, int height,
		Func<WorldCompilerRect, bool> occupied)
	{
		if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
		// Keep the native temple-side anchor when safe; otherwise nearby rings.
		for (int radius = 0; radius <= 8; radius++)
		for (int dy = -radius; dy <= radius; dy++)
		for (int dx = -radius; dx <= radius; dx++)
		{
			if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
			int cx = x + dx * 128, cy = y + dy * 128;
			WorldCompilerRect area = Footprint(cx, cy, direction);
			if (area.X < 100 || area.Right >= width - 100 || area.Y < height / 3 ||
				area.Bottom >= height - 320 || occupied(area)) continue;
			return (cx, cy);
		}
		throw new InvalidOperationException($"SOTS big geode has no protected-geometry-free site near {x},{y}.");
	}
}
