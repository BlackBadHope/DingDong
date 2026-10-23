using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class SotsResourceScale
{
	// SOTS uses discrete Small/Medium/Large targets in a 1:1.5:2 ratio,
	// not the 1:2.286:4 world-area ratio. Preserve its original choices within
	// supported sizes; beyond Large extend the width trend from the Large anchor.
	internal static float BeyondLarge(float original, int width)
	{
		if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
		return width <= 8400 ? original : original * (width / 8400f);
	}

	internal static int BeyondLarge(int original, int width) =>
		checked((int)BeyondLarge((float)original, width));
}
