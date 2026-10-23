using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfaceColumnIndex
	{
		public List<(int Min, int Max)> StartHallowRanges(int width, ResidenceMacroWindow macroWindow)
		{
			List<(int Min, int Max)> ranges = StartRanges(HomeBiome.Hallow, width);
			if (macroWindow is ResidenceMacroWindow.Any or ResidenceMacroWindow.HallowEitherShelf || ranges.Count == 0)
				return ranges;
			ranges.Sort((left, right) => left.Min.CompareTo(right.Min));
			if (ranges.Count == 1)
			{
				(int min, int max) = ranges[0];
				int midpoint = min + (max - min) / 2;
				int separation = Math.Max(64, width / 6);
				return macroWindow == ResidenceMacroWindow.HallowWestShelf
					? ClipRange(min, midpoint - separation / 2)
					: ClipRange(midpoint + separation / 2, max);
			}

			int splitAfter = 0;
			int widestGap = int.MinValue;
			for (int i = 0; i + 1 < ranges.Count; i++)
			{
				int gap = ranges[i + 1].Min - ranges[i].Max - 1;
				if (gap > widestGap)
				{
					widestGap = gap;
					splitAfter = i;
				}
			}
			return macroWindow == ResidenceMacroWindow.HallowWestShelf
				? ranges.GetRange(0, splitAfter + 1)
				: ranges.GetRange(splitAfter + 1, ranges.Count - splitAfter - 1);
		}

		private static List<(int Min, int Max)> ClipRange(int min, int max) =>
			min <= max ? new List<(int, int)> { (min, max) } : new List<(int, int)>();
	}
}
