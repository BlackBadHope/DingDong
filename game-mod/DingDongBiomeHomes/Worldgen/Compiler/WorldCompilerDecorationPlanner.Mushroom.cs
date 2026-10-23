using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerDecorationPlanner
{
	private static void AddMushroomCaverns(int height, WorldCompilerCavePlan caves,
		WorldCompilerSpatialIndex occupied, List<WorldCompilerDecoration> entries)
	{
		int target = 0;
		foreach (WorldCompilerCaveNode node in caves.Nodes)
		{
			if (node.Y < height * 38 / 100 || node.Y > height * 66 / 100)
				continue;
			var entry = new WorldCompilerDecoration(
				WorldCompilerDecorationKind.MushroomCavern, node.X, node.Y + 36,
				176, 82);
			if (!occupied.TryReserve(entry.Bounds, 24)) continue;
			entries.Add(entry);
			if (++target == 4) return;
		}
		throw new InvalidOperationException("Mushroom cavern plan is empty.");
	}
}
