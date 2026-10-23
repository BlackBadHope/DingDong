using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerBandPlanner
{
	private static readonly (int Permille, WorldCompilerBiome Biome)[] Canonical =
	[
		(50, WorldCompilerBiome.Ocean),
		(200, WorldCompilerBiome.Sahara),
		(32, WorldCompilerBiome.Transition),
		(171, WorldCompilerBiome.Arctic),
		(118, WorldCompilerBiome.TemperateForest),
		(221, WorldCompilerBiome.Amazon),
		(158, WorldCompilerBiome.TemperateForest),
		(50, WorldCompilerBiome.Ocean)
	];

	internal static WorldCompilerBand[] Create(int width, ulong seed)
	{
		if (width < 4_000)
			throw new ArgumentOutOfRangeException(nameof(width));
		bool mirror = (WorldCompilerSeed.At(seed, width, 1) & 1UL) != 0;
		var bands = new WorldCompilerBand[Canonical.Length];
		int left = 0;
		for (int index = 0; index < bands.Length; index++)
		{
			int source = mirror ? bands.Length - index - 1 : index;
			int right = index == bands.Length - 1 ? width :
				left + width * Canonical[source].Permille / 1_000;
			bands[index] = new WorldCompilerBand(left, right,
				Canonical[source].Biome);
			left = right;
		}
		Validate(bands, width);
		return bands;
	}

	private static void Validate(WorldCompilerBand[] bands, int width)
	{
		int cursor = 0;
		bool spawnForest = false;
		foreach (WorldCompilerBand band in bands)
		{
			if (band.Left != cursor)
				throw new InvalidOperationException("World bands are not contiguous.");
			cursor = band.Right;
			spawnForest |= band.Biome == WorldCompilerBiome.TemperateForest &&
				band.Contains(width / 2);
		}
		if (cursor != width || !spawnForest)
			throw new InvalidOperationException("World bands failed coverage or spawn forest.");
	}
}
