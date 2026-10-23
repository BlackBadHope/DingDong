using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerDecorationPlanner
{
	private static void AddSurfaceChests(WorldCompilerBand band,
		WorldCompilerSurfaceProfile surface, WorldCompilerSpatialIndex occupied,
		List<WorldCompilerDecoration> entries)
	{
		// Keep the original one-site-per-960 cadence and reward type. Relocation
		// gives that site one chest without changing loot tables or adding sites.
		for (int x = band.Left + 480; x < band.Right - 240; x += 960)
		for (int attempt = 0; attempt < 65; attempt++)
		{
			int distance = (attempt + 1) / 2 * 16;
			int candidate = x + (attempt % 2 == 1 ? -distance : distance);
			if (candidate - 19 < band.Left || candidate + 19 >= band.Right) continue;
			var entry = new WorldCompilerDecoration(
				WorldCompilerDecorationKind.SurfaceChest, candidate,
				surface[candidate] - 1, 6, 5);
			if (!occupied.TryReserve(entry.Bounds, 16)) continue;
			entries.Add(entry);
			break;
		}
	}
}
