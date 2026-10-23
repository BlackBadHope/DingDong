using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeCoastalPolicy
{
	// Calamity 2.2.2 CreateBeach extends 150..190 cells, plus 85 on sand.
	// Thorium may extend its surface replacement/platforms beyond its scan range.
	internal const int BeachAndGraveyardMargin = 190 + 85 + 64;
	internal static int ClipScan(int value, bool lower, int width, int dungeonX,
		int seaWidth, bool active)
	{
		if (!active || dungeonX <= 0 || seaWidth <= 0) return value;
		int coast = seaWidth + BeachAndGraveyardMargin;
		return dungeonX < width / 2
			? (lower ? Math.Max(value, coast) : value)
			: (lower ? value : Math.Min(value, width - coast));
	}
	internal static int Clearance(int native, int height, bool active) =>
		active && height > 2400 ? checked((int)Math.Ceiling(native * (height / 2400d))) : native;
}
