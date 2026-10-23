using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeCoastPlacementPolicy
{
	internal static int DungeonAnchor(int native, int width, int oceanWidth) =>
		Math.Clamp(native, oceanWidth + width / 12, width - oceanWidth - width / 12);
	internal static bool InCoast(int left, int right, int width, int oceanWidth, int padding) =>
		left - padding < oceanWidth || right + padding > width - oceanWidth;
}
