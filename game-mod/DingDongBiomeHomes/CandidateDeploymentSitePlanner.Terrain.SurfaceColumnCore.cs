using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfaceColumnIndex
	{
		private readonly int[] floorY;
		private readonly int[] waterlineY;
		private readonly bool[][] match;
		private readonly int forestHallowSplit;
		private readonly int hallowMax;
		public int HallowMax => hallowMax;

		private SurfaceColumnIndex(int[] floorY, int[] waterlineY, bool[][] match, int forestHallowSplit, int hallowMax)
		{
			this.floorY = floorY;
			this.waterlineY = waterlineY;
			this.match = match;
			this.forestHallowSplit = forestHallowSplit;
			this.hallowMax = hallowMax;
		}

		public bool TryFloor(int x, out int y)
		{
			if ((uint)x >= (uint)floorY.Length || floorY[x] <= 0)
			{
				y = 0;
				return false;
			}
			y = floorY[x];
			return true;
		}

		public bool TryWaterline(int x, out int y)
		{
			if ((uint)x >= (uint)waterlineY.Length || waterlineY[x] <= 0)
			{
				y = 0;
				return false;
			}
			y = waterlineY[x];
			return true;
		}

		public bool TryMeasureRelief(int left, int right, out int relief)
		{
			left = Math.Max(20, left);
			right = Math.Min(floorY.Length - 20, right);
			if (left >= right)
			{
				relief = 0;
				return false;
			}

			int step = Math.Max(4, (right - left) / 24);
			int minimum = int.MaxValue;
			int maximum = int.MinValue;
			int samples = 0;
			for (int x = left; x < right; x += step)
			{
				int y = floorY[x];
				if (y <= 0)
					continue;
				minimum = Math.Min(minimum, y);
				maximum = Math.Max(maximum, y);
				samples++;
			}

			relief = samples == 0 ? 0 : maximum - minimum;
			return samples >= Math.Min(3, Math.Max(1, (right - left) / step));
		}

		public bool MatchesContourColumn(int x, HomeBiome biome, bool allowCoastalGrade)
		{
			if ((uint)x >= (uint)floorY.Length || floorY[x] <= 0)
				return false;
			if (biome == HomeBiome.Ocean && allowCoastalGrade)
			{
				SurfaceBiomeSample sample = SampleSurfaceBiome(x, floorY[x]);
				return MatchesSurfaceBiomeSample(x, biome, sample, out _,
					requireOceanWater: false, allowWeatheredOpenCoast: true);
			}
			int ordinal = (int)biome;
			return ordinal >= 0 && ordinal < match.Length && match[ordinal] != null &&
				match[ordinal][x] && ApplyDistrictPartition(biome, x, matches: true);
		}

		public bool TryMeasureContourRelief(int originX, IReadOnlyList<Point> contour,
			out int relief)
		{
			int minimum = int.MaxValue;
			int maximum = int.MinValue;
			int previousX = int.MinValue;
			int columns = 0;
			foreach (Point local in contour)
			{
				if (local.X == previousX)
					continue;
				previousX = local.X;
				int x = originX + local.X;
				if ((uint)x >= (uint)floorY.Length || floorY[x] <= 0)
					continue;
				minimum = Math.Min(minimum, floorY[x]);
				maximum = Math.Max(maximum, floorY[x]);
				columns++;
			}
			relief = columns == 0 ? 0 : maximum - minimum;
			return columns > 0;
		}
	}
}
