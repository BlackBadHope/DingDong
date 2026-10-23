namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerSeed
{
	internal static ulong Mix(ulong value)
	{
		value += 0x9E3779B97F4A7C15UL;
		value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
		value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
		return value ^ (value >> 31);
	}

	internal static ulong At(ulong seed, int x, int stream)
	{
		ulong coordinate = ((ulong)(uint)x << 32) | (uint)stream;
		return Mix(seed ^ Mix(coordinate));
	}

	internal static int Range(ulong seed, int x, int stream, int minimum,
		int maximumExclusive)
	{
		if (maximumExclusive <= minimum)
			return minimum;
		ulong width = (ulong)(maximumExclusive - minimum);
		return minimum + (int)(At(seed, x, stream) % width);
	}

	internal static double SignedUnit(ulong seed, int x, int stream)
	{
		const double scale = 1.0 / (1UL << 53);
		double unit = (At(seed, x, stream) >> 11) * scale;
		return unit * 2.0 - 1.0;
	}
}
