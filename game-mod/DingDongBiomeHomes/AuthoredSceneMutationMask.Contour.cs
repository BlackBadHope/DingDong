using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public sealed partial class AuthoredSceneMutationMask
{
	private static Point[] BuildExteriorContourCells(int width, int height, bool[] mutationCells)
	{
		bool[] exterior = new bool[checked(width * height)];
		Queue<int> pending = new();
		for (int x = 0; x < width; x++)
		{
			EnqueueExterior(x, 0, width, height, mutationCells, exterior, pending);
			EnqueueExterior(x, height - 1, width, height, mutationCells, exterior, pending);
		}
		for (int y = 1; y + 1 < height; y++)
		{
			EnqueueExterior(0, y, width, height, mutationCells, exterior, pending);
			EnqueueExterior(width - 1, y, width, height, mutationCells, exterior, pending);
		}

		while (pending.Count > 0)
		{
			int index = pending.Dequeue();
			int x = index / height;
			int y = index % height;
			EnqueueExterior(x - 1, y, width, height, mutationCells, exterior, pending);
			EnqueueExterior(x + 1, y, width, height, mutationCells, exterior, pending);
			EnqueueExterior(x, y - 1, width, height, mutationCells, exterior, pending);
			EnqueueExterior(x, y + 1, width, height, mutationCells, exterior, pending);
		}

		List<Point> contour = new();
		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				if (mutationCells[x * height + y] && TouchesExterior(x, y, width, height, exterior))
					contour.Add(new Point(x, y));
		return contour.ToArray();
	}

	private static void EnqueueExterior(int x, int y, int width, int height,
		bool[] mutationCells, bool[] exterior, Queue<int> pending)
	{
		if (x < 0 || y < 0 || x >= width || y >= height)
			return;
		int index = x * height + y;
		if (mutationCells[index] || exterior[index])
			return;
		exterior[index] = true;
		pending.Enqueue(index);
	}

	private static bool TouchesExterior(int x, int y, int width, int height, bool[] exterior) =>
		x == 0 || x + 1 == width || y == 0 || y + 1 == height ||
		exterior[(x - 1) * height + y] || exterior[(x + 1) * height + y] ||
		exterior[x * height + y - 1] || exterior[x * height + y + 1];
}
