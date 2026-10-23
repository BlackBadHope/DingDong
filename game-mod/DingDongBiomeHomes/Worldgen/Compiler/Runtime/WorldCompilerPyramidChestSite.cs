using System;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerPyramidChestSite
{
	// Bounded search within the original pyramid; never patch arena terrain or erase tiles.
	internal static (int X, int Y) Find(WorldCompilerRect bounds, Func<int, int, bool> admissible)
	{
		for (int y = bounds.Bottom - 5; y >= bounds.Y + 5; y--)
		for (int x = bounds.Right - 9; x >= bounds.X + 8; x--)
			if (admissible(x, y)) return (x, y);
		throw new InvalidOperationException($"Pyramid has no safe supported chest site after arena: {bounds}.");
	}
}
