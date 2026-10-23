using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerTerrainSampler
{
	internal static int SeaLevel(WorldCompilerPlan plan) => plan.Height * 18 / 100;

	internal static WorldCompilerBiome BiomeAt(WorldCompilerPlan plan, int x)
	{
		if ((uint)x >= (uint)plan.Width)
			throw new ArgumentOutOfRangeException(nameof(x));
		foreach (WorldCompilerBand band in plan.Bands)
			if (band.Contains(x)) return band.Biome;
		throw new InvalidOperationException("Macro plan has an uncovered column.");
	}

	internal static WorldCompilerTerrainMaterial At(WorldCompilerPlan plan,
		int x, int y)
	{
		if ((uint)y >= (uint)plan.Height)
			throw new ArgumentOutOfRangeException(nameof(y));
		WorldCompilerBiome biome = BiomeAt(plan, x);
		int ground = plan.Surface[x];
		if (y < ground)
			return biome == WorldCompilerBiome.Ocean && y >= SeaLevel(plan) ?
				WorldCompilerTerrainMaterial.Water : WorldCompilerTerrainMaterial.Air;
		return BelowGround(plan.Height, biome, y - ground, y);
	}

	internal static WorldCompilerTerrainMaterial BelowGround(int height,
		WorldCompilerBiome biome, int depth, int y)
	{
		if (y >= height - Math.Max(320, height / 14))
			return WorldCompilerTerrainMaterial.Ash;
		return biome switch
		{
			WorldCompilerBiome.Sahara => Sahara(height, depth),
			WorldCompilerBiome.Arctic => Arctic(height, depth),
			WorldCompilerBiome.Amazon => Amazon(height, depth),
			WorldCompilerBiome.Ocean => Ocean(height, depth),
			_ => Forest(height, depth)
		};
	}

	private static WorldCompilerTerrainMaterial Sahara(int height, int depth)
	{
		if (depth < Math.Max(90, height / 48)) return WorldCompilerTerrainMaterial.Sand;
		if (depth < Math.Max(220, height / 20))
			return WorldCompilerTerrainMaterial.HardenedSand;
		if (depth < Math.Max(420, height / 10))
			return WorldCompilerTerrainMaterial.Sandstone;
		return WorldCompilerTerrainMaterial.Stone;
	}

	private static WorldCompilerTerrainMaterial Arctic(int height, int depth)
	{
		if (depth == 0 || depth < Math.Max(180, height / 24))
			return WorldCompilerTerrainMaterial.Snow;
		if (depth < Math.Max(420, height / 11))
			return WorldCompilerTerrainMaterial.Ice;
		return WorldCompilerTerrainMaterial.Stone;
	}

	private static WorldCompilerTerrainMaterial Amazon(int height, int depth)
	{
		if (depth == 0) return WorldCompilerTerrainMaterial.JungleGrass;
		if (depth < Math.Max(480, height / 9)) return WorldCompilerTerrainMaterial.Mud;
		return WorldCompilerTerrainMaterial.Stone;
	}

	private static WorldCompilerTerrainMaterial Ocean(int height, int depth) =>
		depth < Math.Max(180, height / 22) ? WorldCompilerTerrainMaterial.Sand :
		WorldCompilerTerrainMaterial.Stone;

	private static WorldCompilerTerrainMaterial Forest(int height, int depth)
	{
		if (depth == 0) return WorldCompilerTerrainMaterial.Grass;
		return depth < Math.Max(260, height / 18) ?
			WorldCompilerTerrainMaterial.Dirt : WorldCompilerTerrainMaterial.Stone;
	}
}
