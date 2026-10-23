using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerStructurePlanner
{
	private static void AddJungleResidencePlot(WorldCompilerBand[] bands,
		WorldCompilerSurfaceProfile surface, List<WorldCompilerRoom> rooms,
		WorldCompilerSpatialIndex occupied)
	{
		WorldCompilerBand jungle = FindBand(bands, WorldCompilerBiome.Amazon);
		// Aegis8_JungleTreeCity is 270x350. Reserve terrain, not a prebuilt
		// chamber: natural terrain remains, but later lakes/trees/providers must
		// leave room for the authored landmark and its complete mutation mask.
		for (int x = jungle.Left + 400; x < jungle.Right - 400; x += 32)
		{
			int low = surface[x], high = low;
			for (int dx = -256; dx <= 256; dx++)
			{
				low = Math.Min(low, surface[x + dx]);
				high = Math.Max(high, surface[x + dx]);
			}
			if (high - low > 80) continue;
			var bounds = new WorldCompilerRect(x - 256, low - 400,
				512, high - low + 800);
			if (!occupied.TryReserve(bounds, 24)) continue;
			rooms.Add(new WorldCompilerRoom(rooms.Count,
				WorldCompilerStructureKind.JungleResidencePlot, bounds));
			return;
		}
		throw new InvalidOperationException("No collision-free jungle residence plot.");
	}
}
