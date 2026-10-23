using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class NativeUnderworldPolicy
{
	internal const int CurrentVersion = 2;
	internal static bool IsSupportedVersion(int version) => version is >= 0 and <= CurrentVersion;
	internal static int SavedOffset(int native, int height, int version) =>
		version == 1 && height > 2400 && native > 20
			? checked((int)Math.Round(native * (height / 2400d)))
			: Offset(native, height, version == CurrentVersion);
	// Double the overall biome depth at most. Continental height is x3, but the
	// requested Underworld is x2 and must not become a proportional empty belt.
	internal static int Offset(int native, int height, bool enabled) =>
		!enabled || height <= 2400 || native <= 20 ? native :
		checked((int)Math.Round(native * Math.Min(2d, height / 2400d)));

	// Move a random depth band with the expanded biome while preserving its
	// native local relief amplitude instead of stretching every hill/void.
	internal static int DepthDraw(int native, int baseline, int height, bool enabled) =>
		!enabled || height <= 2400 ? native :
		checked(Offset(baseline, height, true) + native - baseline);
}
