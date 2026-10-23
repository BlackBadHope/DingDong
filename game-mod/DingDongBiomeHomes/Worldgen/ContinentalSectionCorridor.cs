using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalSectionCorridor
{
	internal static int Visit(int startX, int startY, int endX, int endY,
		Func<int, int, bool> visitor)
	{
		int x = startX;
		int y = startY;
		int deltaX = Math.Abs(endX - startX);
		int deltaY = -Math.Abs(endY - startY);
		int stepX = startX < endX ? 1 : -1;
		int stepY = startY < endY ? 1 : -1;
		int error = deltaX + deltaY;
		int visited = 0;
		while (true)
		{
			visited++;
			if (!visitor(x, y) || x == endX && y == endY)
				return visited;
			int twiceError = error * 2;
			if (twiceError >= deltaY)
			{
				error += deltaY;
				x += stepX;
			}
			if (twiceError <= deltaX)
			{
				error += deltaX;
				y += stepY;
			}
		}
	}
}
