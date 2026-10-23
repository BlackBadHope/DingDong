using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ContinentalStructureWorkBudget
{
	// A larger world receives more independent landmarks, not a temple whose
	// room count grows with the full continental width. Keep Large's local
	// geometry; the provider still applies its FTW room multiplier afterwards.
	internal const double TempleScaleLimit = 2d;
	internal const int VanillaTemplePathCapacity = 512;
	internal const int CalamityTemplePathCapacity = 512;
	internal const int TempleBaseRoomLimit = 32;
	internal const int SotsLegacyWidthLimit = 8000;
	internal const int JungleChestAttemptLimit =
		ContinentalWorldDimensions.VanillaLargeWidth * 10;

	internal static double BoundTempleScale(double original) =>
		ContinentalAdapterMath.Active() ? Math.Min(original, TempleScaleLimit) : original;

	internal static float BoundTempleScale(float original) =>
		ContinentalAdapterMath.Active() ? Math.Min(original, (float)TempleScaleLimit) : original;

	internal static int VanillaTempleCapacity() => ContinentalAdapterMath.Active()
		? VanillaTemplePathCapacity : 100;

	internal static int CalamityTempleCapacity() => ContinentalAdapterMath.Active()
		? CalamityTemplePathCapacity : 200;

	internal static int BoundTempleBaseRooms(int original) => ContinentalAdapterMath.Active()
		? Math.Min(original, TempleBaseRoomLimit) : original;

	internal static int BoundSotsLegacyWidth(int original) => Compiler.Runtime.WorldCompilerActivation.IsActive
		? Math.Min(original, SotsLegacyWidthLimit) : original;

	internal static int BoundJungleChestAttempts(int original) => Compiler.Runtime.WorldCompilerActivation.IsActive
		? Math.Min(original, JungleChestAttemptLimit) : original;
}
