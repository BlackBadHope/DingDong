using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerInfectionWindow
{
	internal static (int Left, int Right) HorizontalBounds(int width, int height,
		WorldCompilerInfection infection)
	{
		int maximumDepth = height * 84 / 100;
		int drift = maximumDepth * Math.Abs(infection.SlopePermille) / 1_000;
		int leftDrift = infection.SlopePermille < 0 ? drift : 0;
		int rightDrift = infection.SlopePermille > 0 ? drift : 0;
		int left = Math.Max(0,
			infection.SurfaceX - infection.HalfWidth - leftDrift);
		int right = Math.Min(width,
			infection.SurfaceX + infection.HalfWidth + rightDrift + 1);
		return (left, right);
	}

	internal static bool TryDepthRange(WorldCompilerInfection infection, int x,
		int maximumDepth, out int start, out int end)
	{
		start = 0;
		end = 0;
		if (maximumDepth <= 0) return false;
		int slope = infection.SlopePermille;
		if (slope == 0)
		{
			if (Math.Abs(x - infection.SurfaceX) > infection.HalfWidth)
				return false;
			end = maximumDepth;
			return true;
		}

		long relative = slope > 0 ? x - infection.SurfaceX :
			infection.SurfaceX - x;
		long low = relative - infection.HalfWidth;
		long high = relative + infection.HalfWidth;
		if (high < 0) return false;
		long magnitude = Math.Abs((long)slope);
		long first = low <= 0 ? 0 : DivideCeiling(low * 1_000, magnitude);
		long last = ((high + 1) * 1_000 - 1) / magnitude;
		start = (int)Math.Min(maximumDepth, first);
		end = (int)Math.Min(maximumDepth, last + 1);
		return start < end;
	}

	private static long DivideCeiling(long value, long divisor) =>
		(value + divisor - 1) / divisor;
}
