using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerStructurePlanner
{
	private static int FloatingIslandX(int width, int margin, int original,
		int y, WorldCompilerSpatialIndex occupied)
	{
		// Native SOTS starter homes scan down from y=0 near spawn (x +/-81).
		// Keep a wider clear column for the house/terrain envelope, so they find
		// the continental surface rather than bulldozing a progression island.
		var spawnAir = new WorldCompilerRect(width / 2 - 256, 0, 512, y + 100);
		for (int attempt = 0; attempt < 65; attempt++)
		{
			int distance = (attempt + 1) / 2 * 32;
			int x = original + (attempt % 2 == 1 ? -distance : distance);
			var bounds = new WorldCompilerRect(x - 70, y - 28, 140, 56);
			if (bounds.X < margin || bounds.Right > width - margin ||
				bounds.Inflate(24).Intersects(spawnAir) ||
				occupied.Overlaps(bounds.Inflate(24))) continue;
			return x;
		}
		throw new InvalidOperationException("No collision-free island outside spawn sky.");
	}
}
