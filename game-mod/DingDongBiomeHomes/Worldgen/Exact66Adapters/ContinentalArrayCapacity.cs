using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ContinentalArrayCapacity
{
	internal static void Grow<T>(ref T[] values, int capacity)
	{
		if (values == null || values.Length < capacity)
			Array.Resize(ref values, capacity);
	}
}
