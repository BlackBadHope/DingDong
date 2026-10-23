using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class MushroomExclusionScan
{
	// Exact type set and stride from Terraria's Mushroom Patches predicate.
	// The original checks type even for inactive tiles; preserve that behavior.
	internal static bool Rejected(int x, int y, int radius, int width, int height,
		Func<int, int, ushort> typeAt, Func<int, int, bool> inDesert)
	{
		for (int i = x - radius; i < x + radius; i += 3)
			for (int j = y - radius; j < y + radius; j += 3)
			{
				if ((uint)i >= (uint)width || (uint)j >= (uint)height) return true;
				ushort type = typeAt(i, j);
				if (type is 147 or 161 or 162 or 60 or 368 or 367 || inDesert(i, j)) return true;
			}
		return false;
	}
}
