using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerDecorationPlanner
{
	internal static WorldCompilerDecorationPlan Create(int width, int height,
		ulong seed, WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface,
		WorldCompilerCavePlan caves, WorldCompilerStructurePlan structures,
		WorldCompilerHydrologyPlan hydrology)
	{
		var entries = new List<WorldCompilerDecoration>();
		var occupied = WorldCompilerProtectedGeometry.Create(surface, structures, 20);
		foreach (WorldCompilerBasin basin in hydrology.Basins)
			occupied.ReserveExisting(basin.Bounds.Inflate(8));
		AddSurface(seed, bands, surface, occupied, entries);
		AddMushroomCaverns(height, caves, occupied, entries);
		AddCaveRewards(height, seed, caves, occupied, entries);
		return new WorldCompilerDecorationPlan(entries.ToArray(), Fingerprint(entries));
	}

	private static void AddSurface(ulong seed, WorldCompilerBand[] bands,
		WorldCompilerSurfaceProfile surface, WorldCompilerSpatialIndex occupied,
		List<WorldCompilerDecoration> entries)
	{
		foreach (WorldCompilerBand band in bands)
		{
			if (band.Biome == WorldCompilerBiome.Amazon)
				AddTitanTrees(seed, band, surface, occupied, entries);
			// Fixed-frequency rewards get sites before dense optional foliage.
			if (band.Biome is not WorldCompilerBiome.Ocean)
				AddSurfaceChests(band, surface, occupied, entries);
			int spacing = band.Biome switch
			{
				WorldCompilerBiome.Amazon => 38,
				WorldCompilerBiome.Sahara => 30,
				WorldCompilerBiome.Arctic => 44,
				WorldCompilerBiome.TemperateForest => 34,
				_ => band.Width + 1
			};
			int ordinal = 0;
			for (int baseX = band.Left + spacing; baseX < band.Right - spacing;
				baseX += spacing, ordinal++)
			{
				int x = baseX + WorldCompilerSeed.Range(seed,
					band.Left + ordinal, 601, -spacing / 3, spacing / 3 + 1);
				AddSurfaceEntry(seed, band.Biome, ordinal, x, surface[x], occupied,
					entries);
			}
		}
	}

	private static void AddSurfaceEntry(ulong seed, WorldCompilerBiome biome,
		int ordinal, int x, int ground, WorldCompilerSpatialIndex occupied,
		List<WorldCompilerDecoration> entries)
	{
		WorldCompilerDecorationKind kind = biome switch
		{
			WorldCompilerBiome.Amazon => WorldCompilerDecorationKind.AmazonTree,
			WorldCompilerBiome.Arctic => WorldCompilerDecorationKind.ArcticPine,
			WorldCompilerBiome.Sahara => WorldCompilerDecorationKind.Cactus,
			_ => WorldCompilerDecorationKind.ForestTree
		};
		int height = kind == WorldCompilerDecorationKind.Cactus ?
			WorldCompilerSeed.Range(seed, ordinal, 607, 4, 9) :
			WorldCompilerSeed.Range(seed, ordinal, 607, 10, 23);
		int width = kind == WorldCompilerDecorationKind.Cactus ? 3 : 13;
		var entry = new WorldCompilerDecoration(kind, x, ground - 1, width, height);
		if (occupied.TryReserve(entry.Bounds, 3)) entries.Add(entry);
	}

	private static void AddCaveRewards(int height, ulong seed,
		WorldCompilerCavePlan caves, WorldCompilerSpatialIndex occupied,
		List<WorldCompilerDecoration> entries)
	{
		for (int index = 4; index < caves.Nodes.Length; index += 7)
		{
			WorldCompilerCaveNode node = caves.Nodes[index];
			WorldCompilerDecorationKind kind = index % 21 == 4 ?
				WorldCompilerDecorationKind.LifeCrystal : node.Y > height * 67 / 100 ?
				WorldCompilerDecorationKind.DeepChest :
				WorldCompilerDecorationKind.UndergroundChest;
			int y = kind == WorldCompilerDecorationKind.LifeCrystal ? node.Y :
				node.Y + node.Radius;
			var entry = new WorldCompilerDecoration(kind, node.X - 1, y, 6, 5);
			if (occupied.TryReserve(entry.Bounds, 10)) entries.Add(entry);
		}
	}

}
