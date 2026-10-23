using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerHydrologyPlanner
{
	internal static WorldCompilerHydrologyPlan Create(int width, int height,
		ulong seed, WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface,
		WorldCompilerStructurePlan structures)
	{
		var basins = new List<WorldCompilerBasin>();
		AddSurfaceLakes(width, seed, bands, surface, structures, basins);
		AddAquifers(width, height, seed, bands, structures, basins);
		return new WorldCompilerHydrologyPlan(basins.ToArray(), Fingerprint(basins));
	}

	private static void AddSurfaceLakes(int width, ulong seed,
		WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface,
		WorldCompilerStructurePlan structures, List<WorldCompilerBasin> basins)
	{
		for (int x = 360; x < width - 360; x += 720)
		{
			WorldCompilerBiome biome = BiomeAt(bands, x);
			if (biome is WorldCompilerBiome.Sahara or WorldCompilerBiome.Arctic or
				WorldCompilerBiome.Ocean) continue;
			int key = x / 720;
			int radiusX = WorldCompilerSeed.Range(seed, key, 401, 34, 74);
			int radiusY = WorldCompilerSeed.Range(seed, key, 403, 12, 25);
			var basin = new WorldCompilerBasin(x, surface[x] + radiusY / 2,
				radiusX, radiusY, biome == WorldCompilerBiome.Amazon &&
				WorldCompilerSeed.At(seed, key, 405) % 5UL == 0 ?
				WorldCompilerFluid.Honey : WorldCompilerFluid.Water);
			if (!Overlaps(structures, basin.Bounds.Inflate(18))) basins.Add(basin);
		}
	}

	private static void AddAquifers(int width, int height, ulong seed,
		WorldCompilerBand[] bands, WorldCompilerStructurePlan structures,
		List<WorldCompilerBasin> basins)
	{
		WorldCompilerPoint[] points = WorldCompilerCandidateLattice.Create(
			new WorldCompilerRect(220, height / 3, width - 440, height * 5 / 9),
			620, 420, seed ^ 0x485944524FUL);
		for (int index = 0; index < points.Length; index++)
		{
			WorldCompilerPoint point = points[index];
			int rx = WorldCompilerSeed.Range(seed, index, 409, 18, 46);
			int ry = WorldCompilerSeed.Range(seed, index, 411, 10, 28);
			WorldCompilerBiome biome = BiomeAt(bands, point.X);
			WorldCompilerFluid fluid = point.Y > height * 79 / 100 ?
				WorldCompilerFluid.Lava : biome == WorldCompilerBiome.Amazon &&
				WorldCompilerSeed.At(seed, index, 413) % 7UL == 0 ?
				WorldCompilerFluid.Honey : WorldCompilerFluid.Water;
			var basin = new WorldCompilerBasin(point.X, point.Y, rx, ry, fluid);
			if (!Overlaps(structures, basin.Bounds.Inflate(12))) basins.Add(basin);
		}
	}

	private static WorldCompilerBiome BiomeAt(WorldCompilerBand[] bands, int x)
	{
		foreach (WorldCompilerBand band in bands)
			if (band.Contains(x)) return band.Biome;
		return WorldCompilerBiome.TemperateForest;
	}

	private static bool Overlaps(WorldCompilerStructurePlan plan,
		WorldCompilerRect area)
	{
		foreach (WorldCompilerRoom room in plan.Rooms)
			if (area.Intersects(room.Bounds)) return true;
		return false;
	}

	private static ulong Fingerprint(List<WorldCompilerBasin> basins)
	{
		ulong hash = 0x485944524F4C4F47UL;
		foreach (WorldCompilerBasin basin in basins)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)basin.X << 32 ^
				(uint)basin.Y ^ (ulong)basin.Fluid);
		return hash;
	}
}
