using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeSurfaceFloorSearch
{
	internal static bool TryFind(int top, int bottom, Func<int, bool> naturalFloor,
		Func<int, bool> clearance, Func<int, bool> mainland, Func<int, bool> support, out int floor)
	{
		// Scan the column from the sky, not from a narrow band around the global
		// worldSurface marker. That band can START INSIDE a continental mountain.
		for (int y = top; y < bottom; y++)
			if (naturalFloor(y) && clearance(y) && mainland(y))
			{
				// A thin mainland roof is still the surface. If it cannot support a
				// residence, reject this column; descending to the next cave floor
				// would bury the surface residence. Only detached islands may be skipped.
				bool supported = support(y);
				floor = supported ? y : 0;
				return supported;
			}
		floor = 0;
		return false;
	}

	internal static int ElevationAllowance(int height) =>
		Math.Max(192, checked((int)((long)192 * height / 2400)));
}
