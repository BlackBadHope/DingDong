using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class NativeTemplePathCapacity
{
	// Use the actual room count AFTER the original special-seed multipliers.
	// Do not consume random numbers or change either room count or geometry.
	internal static T[] Ensure<T>(T[] paths, int rooms)
	{
		if (paths is null) throw new ArgumentNullException(nameof(paths));
		if (rooms < 0) throw new ArgumentOutOfRangeException(nameof(rooms));
		int required = checked(rooms + 1);
		if (paths.Length < required) Array.Resize(ref paths, required);
		return paths;
	}
}
