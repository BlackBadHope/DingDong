using System;
using System.Collections.Concurrent;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerEllipseProfile
{
	private static readonly ConcurrentDictionary<long, int[]> Cache = new();

	internal static int[] Get(int radiusX, int radiusY)
	{
		if (radiusX <= 0 || radiusY <= 0)
			throw new ArgumentOutOfRangeException(nameof(radiusX));
		long key = ((long)radiusX << 32) | (uint)radiusY;
		return Cache.GetOrAdd(key, _ => Create(radiusX, radiusY));
	}

	internal static void Clear() => Cache.Clear();

	private static int[] Create(int radiusX, int radiusY)
	{
		var result = new int[radiusX * 2 + 1];
		long radiusX2 = (long)radiusX * radiusX;
		long radiusY2 = (long)radiusY * radiusY;
		long limit = radiusX2 * radiusY2;
		for (int dx = -radiusX; dx <= radiusX; dx++)
		{
			long remaining = limit - (long)dx * dx * radiusY2;
			int extent = (int)Math.Sqrt(remaining / (double)radiusX2);
			while ((long)(extent + 1) * (extent + 1) * radiusX2 <= remaining)
				extent++;
			while ((long)extent * extent * radiusX2 > remaining)
				extent--;
			result[dx + radiusX] = extent;
		}
		return result;
	}
}
