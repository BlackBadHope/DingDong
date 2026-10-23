using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerSurfaceProfile
{
	private readonly int[] heights;
	internal int Width => heights.Length;
	internal int this[int x] => heights[x];

	private WorldCompilerSurfaceProfile(int[] heights) => this.heights = heights;

	internal static WorldCompilerSurfaceProfile Create(int width, int height,
		ulong seed, WorldCompilerBand[] bands)
	{
		if (width <= 0 || height < 1_200)
			throw new ArgumentOutOfRangeException(nameof(width));
		var result = new int[width];
		int bandIndex = 0;
		for (int x = 0; x < width; x++)
		{
			while (bandIndex + 1 < bands.Length && x >= bands[bandIndex].Right)
				bandIndex++;
			WorldCompilerBiome biome = bands[bandIndex].Biome;
			double broad = Noise(seed, x, 2_048, 11);
			double medium = Noise(seed, x, 640, 17);
			double detail = Noise(seed, x, 176, 23);
			double scale = height / 7_200.0;
			double y = height * 0.18 + broad * 170 * scale +
				medium * 74 * scale + detail * 24 * scale;
			y += BlendedBiomeOffset(bands, bandIndex, x, scale);
			result[x] = Math.Clamp((int)Math.Round(y), height / 12, height / 3);
		}
		return new WorldCompilerSurfaceProfile(result);
	}

	private static double Noise(ulong seed, int x, int wavelength, int stream)
	{
		int cell = x / wavelength;
		double t = (x % wavelength) / (double)wavelength;
		t = t * t * t * (t * (t * 6 - 15) + 10);
		double left = WorldCompilerSeed.SignedUnit(seed, cell, stream);
		double right = WorldCompilerSeed.SignedUnit(seed, cell + 1, stream);
		return left + (right - left) * t;
	}

	private static double BiomeOffset(WorldCompilerBiome biome, double scale) =>
		biome switch
		{
			WorldCompilerBiome.Sahara => 42 * scale,
			WorldCompilerBiome.Arctic => -110 * scale,
			WorldCompilerBiome.Amazon => 68 * scale,
			WorldCompilerBiome.Ocean => 150 * scale,
			_ => 0
		};

	private static double BlendedBiomeOffset(WorldCompilerBand[] bands,
		int index, int x, double scale)
	{
		const int blendRadius = 192;
		WorldCompilerBand current = bands[index];
		double own = BiomeOffset(current.Biome, scale);
		if (index > 0 && x < current.Left + blendRadius)
		{
			double t = Smooth((x - (current.Left - blendRadius)) /
				(double)(blendRadius * 2));
			return Lerp(BiomeOffset(bands[index - 1].Biome, scale), own, t);
		}
		if (index + 1 < bands.Length && x >= current.Right - blendRadius)
		{
			double t = Smooth((x - (current.Right - blendRadius)) /
				(double)(blendRadius * 2));
			return Lerp(own, BiomeOffset(bands[index + 1].Biome, scale), t);
		}
		return own;
	}

	private static double Smooth(double value)
	{
		double t = Math.Clamp(value, 0.0, 1.0);
		return t * t * (3.0 - 2.0 * t);
	}

	private static double Lerp(double left, double right, double amount) =>
		left + (right - left) * amount;
}
