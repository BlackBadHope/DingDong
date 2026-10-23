using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

// Re-express an indexed contact-column domain as full-scene left coordinates.
// The natural contact columns stay identical; only their local source X changes.
internal static class SurfaceAnchorRangeProjection
{
	internal static IEnumerable<int> ContactOffsets(int sourceWidth, int footprintLeft, int footprintWidth)
	{
		if (sourceWidth <= 0 || footprintLeft < 0 || footprintWidth <= 0 ||
			(long)footprintLeft + footprintWidth > sourceWidth)
			throw new ArgumentOutOfRangeException(nameof(footprintWidth));
		int first = footprintLeft + footprintWidth / 4;
		int second = footprintLeft + footprintWidth * 3 / 4;
		yield return first;
		if (second != first) yield return second;
	}

	internal static List<(int Min, int Max)> Translate(
		IReadOnlyList<(int Min, int Max)> centerRanges, int sourceWidth,
		int contactOffset, int minimumLeft, int maximumLeft)
	{
		if (sourceWidth <= 0 || contactOffset < 0 || contactOffset >= sourceWidth)
			throw new ArgumentOutOfRangeException(nameof(contactOffset));
		List<(int Min, int Max)> result = new();
		long delta = sourceWidth / 2 - contactOffset;
		foreach ((int min, int max) in centerRanges)
		{
			if (min > max) continue;
			long lo = Math.Max(minimumLeft, (long)min + delta);
			long hi = Math.Min(maximumLeft, (long)max + delta);
			if (lo <= hi) result.Add(((int)lo, (int)hi));
		}
		return result;
	}
}
