using System;

namespace DingDongBiomeHomes.Worldgen;

// Extend Large, not Small: vanilla already supplies the horizontal multiplier.
internal static class NativePoiDensityPolicy
{
	internal static double Underground(double nativeTarget, int height, bool enabled) =>
		enabled ? nativeTarget * Math.Max(1d, height / 2400d) : nativeTarget;

	// Horizontal runs (long minecart tracks) are already counted per world width by
	// vanilla; stacking them by depth produced three tiers of rails per screen on x10.
	internal static int UndergroundCount(int nativeTarget, int height,
		bool enabled, bool widthScaled, bool horizontal = false) =>
		!enabled || !widthScaled || horizontal || height <= 2400
			? nativeTarget : checked((int)Math.Ceiling(Underground(nativeTarget, height, true)));

	internal static int SkyLakes(int nativeTarget, int width, bool enabled) =>
		!enabled || width <= 8400 ? nativeTarget :
			Math.Max(nativeTarget, checked((int)Math.Ceiling(3d * width / 8400d)));
}
