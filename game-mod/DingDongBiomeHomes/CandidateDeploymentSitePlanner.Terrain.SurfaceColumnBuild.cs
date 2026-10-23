using System;
using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfaceColumnIndex
	{
		public static SurfaceColumnIndex Build()
		{
			int width = Main.maxTilesX;
			int[] floors = new int[width];
			int[] waterlines = new int[width];
			for (int x = 0; x < width; x++)
			{
				if (TryFindSurfaceFloor(x, out int y))
					floors[x] = y;
				if (IsInsideCoastalStrip(x) && TryFindCoastalWaterline(x, out int waterline))
					waterlines[x] = waterline;
			}

			bool[][] flags = BuildBiomeFlags(width, floors);
			List<(int Start, int End)> purityRuns = FindPurityRuns(flags, floors, width);
			(int split, int hallowTop) = FindForestHallowBoundary(purityRuns, width);
			return new SurfaceColumnIndex(floors, waterlines, flags, split, hallowTop);
		}

		private static bool[][] BuildBiomeFlags(int width, int[] floors)
		{
			HomeBiome[] biomes = { HomeBiome.Forest, HomeBiome.Desert, HomeBiome.Snow, HomeBiome.Jungle, HomeBiome.Ocean, HomeBiome.Hallow };
			bool[][] flags = new bool[9][];
			int minX = WorldMargin + 20;
			int maxX = width - WorldMargin - 20;
			foreach (HomeBiome biome in biomes)
				flags[(int)biome] = new bool[width];
			for (int x = minX; x < maxX; x++)
			{
				if (floors[x] <= 0) continue;
				SurfaceBiomeSample sample = SampleSurfaceBiome(x, floors[x]);
				foreach (HomeBiome biome in biomes)
					flags[(int)biome][x] = MatchesSurfaceBiomeSample(x, biome, sample, out _);
			}
			return flags;
		}

		private static List<(int Start, int End)> FindPurityRuns(bool[][] flags, int[] floors, int width)
		{
			bool[] forest = flags[(int)HomeBiome.Forest];
			bool[] hallow = flags[(int)HomeBiome.Hallow];
			List<(int Start, int End)> runs = new();
			int runStart = -1;
			int last = -1;
			for (int x = WorldMargin + 20; x < width - WorldMargin - 20; x++)
			{
				bool purity = (forest != null && forest[x]) || (hallow != null && hallow[x]);
				if (purity && floors[x] > 0)
				{
					if (runStart < 0)
						runStart = x;
					last = x;
				}
				else if (runStart >= 0)
				{
					runs.Add((runStart, last));
					runStart = -1;
				}
			}
			if (runStart >= 0)
				runs.Add((runStart, last));
			return runs;
		}

		private static (int Split, int HallowTop) FindForestHallowBoundary(List<(int Start, int End)> runs, int width)
		{
			int split = width * 55 / 100;
			int hallowTop = width - 180;
			(int Start, int End) longest = default;
			(int Start, int End) rightWide = default;
			bool foundLongest = false;
			bool foundRight = false;
			foreach ((int start, int end) in runs)
			{
				int span = end - start + 1;
				if (!foundLongest || span > longest.End - longest.Start + 1)
				{
					longest = (start, end);
					foundLongest = true;
				}
				if (span >= 480 && (!foundRight || start > rightWide.Start))
				{
					rightWide = (start, end);
					foundRight = true;
				}
			}
			if (foundRight)
			{
				int borrowed = Math.Min(420, Math.Max(0, longest.End - longest.Start - 1600));
				split = Math.Min(rightWide.Start, Math.Max(longest.Start, longest.End - borrowed));
				hallowTop = Math.Max(rightWide.End, width - 180);
			}
			return (split, hallowTop);
		}
	}
}
