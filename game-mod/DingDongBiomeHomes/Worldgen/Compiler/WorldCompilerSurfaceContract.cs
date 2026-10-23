using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal delegate bool WorldCompilerSolidProbe(int x, int y);

internal readonly record struct WorldCompilerSurfaceReceipt(int Samples,
	int Missing, int MaximumDrift, int FirstMissingX)
{
	internal bool Complete => Samples > 0 && Missing == 0;
}

internal static class WorldCompilerSurfaceContract
{
	private const int Step = 257;
	private const int HorizontalProbeRadius = 4;
	private const int AbovePlan = 192;
	private const int BelowPlan = 384;

	internal static WorldCompilerSurfaceReceipt Survey(int width, int height,
		WorldCompilerSurfaceProfile plannedSurface, WorldCompilerSolidProbe solid)
	{
		if (plannedSurface == null || plannedSurface.Width != width)
			throw new ArgumentException("Surface plan width mismatch.", nameof(plannedSurface));
		if (solid == null) throw new ArgumentNullException(nameof(solid));
		int samples = 0;
		int missing = 0;
		int maximumDrift = 0;
		int firstMissingX = -1;
		for (int x = 1; x < width - 1; x += Step)
			Sample(x);
		if ((width - 2 - 1) % Step != 0) Sample(width - 2);
		return new WorldCompilerSurfaceReceipt(samples, missing, maximumDrift,
			firstMissingX);

		void Sample(int centerX)
		{
			samples++;
			int expected = plannedSurface[centerX];
			int top = Math.Max(1, expected - AbovePlan);
			int bottom = Math.Min(height - 2, expected + BelowPlan);
			int nearest = int.MaxValue;
			for (int x = Math.Max(1, centerX - HorizontalProbeRadius);
				x <= Math.Min(width - 2, centerX + HorizontalProbeRadius); x++)
			for (int y = top; y <= bottom; y++)
				if (solid(x, y)) nearest = Math.Min(nearest, Math.Abs(y - expected));
			if (nearest != int.MaxValue)
			{
				maximumDrift = Math.Max(maximumDrift, nearest);
				return;
			}
			missing++;
			if (firstMissingX < 0) firstMissingX = centerX;
		}
	}
}
