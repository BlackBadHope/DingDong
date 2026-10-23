using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerBand
{
	internal int Left { get; }
	internal int Right { get; }
	internal WorldCompilerBiome Biome { get; }
	internal int Width => Right - Left;

	internal WorldCompilerBand(int left, int right, WorldCompilerBiome biome)
	{
		if (left < 0 || right <= left)
			throw new ArgumentOutOfRangeException(nameof(right));
		Left = left;
		Right = right;
		Biome = biome;
	}

	internal bool Contains(int x) => x >= Left && x < Right;
}
