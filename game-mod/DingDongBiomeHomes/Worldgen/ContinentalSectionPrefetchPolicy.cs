using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalSectionPrefetchPolicy
{
	private static readonly (int X, int Y)[] Neighborhood =
	[
		(0, 0), (1, 0), (-1, 0), (0, 1), (0, -1),
		(1, 1), (1, -1), (-1, 1), (-1, -1)
	];

	internal static int Visit(int centerX, int centerY, int directionX, int directionY,
		int width, int height, Func<int, int, bool> visitor)
	{
		directionX = Math.Sign(directionX);
		directionY = Math.Sign(directionY);
		int used = 0, visited = 0;
		for (int order = 0; order < Neighborhood.Length; order++)
		{
			int best = -1, bestScore = int.MinValue;
			for (int index = 0; index < Neighborhood.Length; index++)
			{
				if ((used & (1 << index)) != 0) continue;
				(int dx, int dy) = Neighborhood[index];
				int score = index == 0 ? int.MaxValue :
					4 * (dx * directionX + dy * directionY) - Math.Abs(dx) - Math.Abs(dy);
				if (score <= bestScore) continue;
				best = index;
				bestScore = score;
			}
			used |= 1 << best;
			int x = centerX + Neighborhood[best].X;
			int y = centerY + Neighborhood[best].Y;
			if (x < 0 || y < 0 || x >= width || y >= height) continue;
			visited++;
			if (!visitor(x, y)) break;
		}
		return visited;
	}
}
