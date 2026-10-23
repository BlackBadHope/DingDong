using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerDecorationPlanner
{
	private static void AddTitanTrees(ulong seed, WorldCompilerBand band,
		WorldCompilerSurfaceProfile surface, WorldCompilerSpatialIndex occupied,
		List<WorldCompilerDecoration> entries)
	{
		int ordinal = 0;
		for (int x = band.Left + 260; x < band.Right - 260; x += 480, ordinal++)
		{
			int shifted = x + WorldCompilerSeed.Range(seed, ordinal, 613, -90, 91);
			int height = WorldCompilerSeed.Range(seed, ordinal, 617, 140, 241);
			// Keep the original seeded site first. A blocked site has at most 64
			// nearby alternatives; all use the same full reservation and tree size.
			for (int attempt = 0; attempt < 65; attempt++)
			{
				int distance = (attempt + 1) / 2 * 16;
				int candidate = shifted + (attempt % 2 == 1 ? -distance : distance);
				if (candidate - 94 < band.Left || candidate + 94 >= band.Right) continue;
				var entry = new WorldCompilerDecoration(
					WorldCompilerDecorationKind.AmazonTitanTree, candidate,
					surface[candidate] - 1, 132, height);
				if (!occupied.TryReserve(entry.Bounds, 28)) continue;
				entries.Add(entry);
				break;
			}
		}
	}
}
