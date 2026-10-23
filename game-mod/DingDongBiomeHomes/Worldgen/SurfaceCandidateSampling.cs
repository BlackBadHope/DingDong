using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

// Finite, deterministic sampling without consuming the providers' world RNG.
internal static class SurfaceCandidateSampling
{
	internal static IEnumerable<int> Columns(int min, int max, int seed, int step,
		bool spread, bool refine = false)
	{
		if (max < min) yield break;
		int count = checked(max - min + 1);
		if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
		uint positiveSeed = unchecked((uint)seed);
		if (!spread)
		{
			if (refine) yield break;
			int stride = Math.Min(37, count);
			while (Gcd(stride, count) != 1) stride--;
			for (int i = 0; i < count; i++)
				yield return min + (int)((positiveSeed + (long)i * stride) % count);
			yield break;
		}
		int phase = (int)(positiveSeed % (uint)Math.Min(step, count));
		if (!refine)
		{
			for (int offset = phase; offset < count; offset += step)
				yield return min + offset;
		}
		else
		{
			for (int offset = 0; offset < count; offset++)
				if (offset % step != phase) yield return min + offset;
		}
	}

	private static int Gcd(int a, int b)
	{
		while (b != 0) (a, b) = (b, a % b);
		return a;
	}
}
