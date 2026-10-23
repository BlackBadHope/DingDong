using System;
using System.Numerics;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalScaleProfile
{
	internal const int ReferenceWidth = ContinentalWorldDimensions.VanillaLargeWidth;
	internal const int ReferenceHeight = ContinentalWorldDimensions.VanillaLargeHeight;
	internal const long ReferenceTiles = (long)ReferenceWidth * ReferenceHeight;

	internal static long AreaScale(long tiles) => Math.Max(1L,
		checked((tiles + ReferenceTiles - 1L) / ReferenceTiles));

	internal static long WidthScale(int width) => Math.Max(1L,
		checked((width + ReferenceWidth - 1L) / ReferenceWidth));

	internal static long HeightScale(int height) => Math.Max(1L,
		checked((height + ReferenceHeight - 1L) / ReferenceHeight));

	internal static int Capacity(long baseline, long scale)
	{
		long required = checked(baseline * Math.Max(1L, scale));
		if (required > 1 << 24)
			throw new InsufficientMemoryException(
				$"Worldgen buffer requirement is unreasonable: {required}.");
		return checked((int)BitOperations.RoundUpToPowerOf2((uint)required));
	}
}
