using System;
using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfaceColumnIndex
	{
		public List<(int Min, int Max)> StartRanges(HomeBiome biome, int width)
		{
			if (biome == HomeBiome.Ocean)
				return SurfaceRanges(biome, width);
			int ordinal = (int)biome;
			if (ordinal < 0 || ordinal >= match.Length || match[ordinal] == null)
				return SurfaceRanges(biome, width);

			bool[] row = match[ordinal];
			List<(int Min, int Max)> ranges = new();
			int runStart = -1;
			int last = -1;
			for (int left = 180; left <= Main.maxTilesX - 180 - width; left++)
			{
				int center = left + width / 2;
				bool matches = (uint)center < (uint)row.Length && row[center] && floorY[center] > 0;
				matches = ApplyDistrictPartition(biome, center, matches);
				if (matches)
				{
					if (runStart < 0)
						runStart = left;
					last = left;
				}
				else if (runStart >= 0)
				{
					ranges.Add((runStart, last));
					runStart = -1;
				}
			}
			if (runStart >= 0)
				ranges.Add((runStart, last));
			return ranges.Count > 0 ? ranges : ClipFallbackRanges(biome, width);
		}

		private bool ApplyDistrictPartition(HomeBiome biome, int center, bool matches)
		{
			// Native Hallow matching already admits the reviewed
			// PuritySurfaceForPreHardmodeHallow contract. A synthetic right-hand
			// district must not remove matching stone/clay purity columns that the
			// narrower Dirt/Grass Forest fallback cannot recover.
			if (biome == HomeBiome.Hallow && Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive)
			{
				bool[] forest = match[(int)HomeBiome.Forest];
				bool[] hallow = match[(int)HomeBiome.Hallow];
				bool purity = forest != null && (uint)center < (uint)forest.Length && forest[center]
					|| hallow != null && (uint)center < (uint)hallow.Length && hallow[center];
				return purity && floorY[center] > 0 && center >= forestHallowSplit && center <= hallowMax;
			}
			if (matches && biome != HomeBiome.Ocean && (center < CoastalStripWidth || center > Main.maxTilesX - CoastalStripWidth))
				matches = false;
			// Native terrain has no authored left-forest/right-hallow district.
			// Reserve compatible houses through the simultaneous solver, not by
			// discarding all real forest columns east of an inferred split.
			if (Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive &&
				matches && biome == HomeBiome.Forest && center >= forestHallowSplit)
				matches = false;
			return matches;
		}

		private List<(int Min, int Max)> ClipFallbackRanges(HomeBiome biome, int width)
		{
			List<(int Min, int Max)> fallback = SurfaceRanges(biome, width);
			if (biome is not HomeBiome.Forest and not HomeBiome.Hallow ||
				!Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive)
				return fallback;
			List<(int Min, int Max)> clipped = new();
			foreach ((int min, int max) in fallback)
			{
				int lo = biome == HomeBiome.Hallow ? Math.Max(min, forestHallowSplit - width / 2 + 1) : min;
				int hi = biome == HomeBiome.Forest ? Math.Min(max, forestHallowSplit - width / 2) : Math.Min(max, hallowMax - width / 2);
				if (lo <= hi)
					clipped.Add((lo, hi));
			}
			return clipped.Count == 0 ? fallback : clipped;
		}
	}
}
