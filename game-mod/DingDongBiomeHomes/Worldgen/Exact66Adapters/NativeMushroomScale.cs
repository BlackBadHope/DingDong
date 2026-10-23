using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

// Native patch count grows with width. Beyond Large, patch AREA must supply
// the remaining height growth, not multiply that count by width squared.
internal static class NativeMushroomScale
{
	internal static double Geometry(double original, int width, int height)
	{
		if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
		if (width <= 8400 && height <= 2400) return original;
		return 2.0 * Math.Sqrt(height / 2400.0);
	}
}
