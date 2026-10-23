using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfaceColumnIndex
	{
		public List<(int Min, int Max)> StartSnowEdge(int width)
		{
			bool[] snow = match[(int)HomeBiome.Snow];
			bool[] forest = match[(int)HomeBiome.Forest];
			bool[] hallow = match[(int)HomeBiome.Hallow];
			FindSnowBounds(snow, out int snowMin, out int snowMax);
			if (snowMin == int.MaxValue)
				return StartRanges(HomeBiome.Snow, width);

			List<(int Min, int Max)> ranges = new();
			int runStart = -1;
			int last = -1;
			for (int left = 180; left <= Main.maxTilesX - CoastalStripWidth - width; left++)
			{
				int center = left + width / 2;
				bool purity = forest != null && (uint)center < (uint)forest.Length && forest[center]
					|| hallow != null && (uint)center < (uint)hallow.Length && hallow[center];
				bool nearSnow = center >= snowMin - 360 && center < snowMin || center > snowMax && center <= snowMax + 360;
				bool accepted = purity && nearSnow && floorY[center] > 0 && center >= CoastalStripWidth && center <= Main.maxTilesX - CoastalStripWidth;
				if (accepted)
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
			return ranges.Count == 0 ? StartRanges(HomeBiome.Snow, width) : ranges;
		}

		private void FindSnowBounds(bool[] snow, out int minimum, out int maximum)
		{
			minimum = int.MaxValue;
			maximum = 0;
			if (snow == null)
				return;
			for (int x = CoastalStripWidth; x < Main.maxTilesX - CoastalStripWidth; x++)
			{
				if (!snow[x] || floorY[x] <= 0)
					continue;
				if (x < minimum)
					minimum = x;
				if (x > maximum)
					maximum = x;
			}
		}
	}
}
