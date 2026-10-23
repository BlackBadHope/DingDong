using System;
using System.Collections.Generic;
using DingDongBiomeHomes.Worldgen.Compiler;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class SotsTempleRoof
{
	internal static (int X, int Y) Find(IReadOnlyList<WorldCompilerRoom> rooms,
		int altarX, int altarY, Func<int, int, bool> isBrick)
	{
		bool altarInTemple = false;
		int bestX = -1, bestY = int.MaxValue, bestLength = 0;
		foreach (WorldCompilerRoom room in rooms)
		{
			if (room.Kind != WorldCompilerStructureKind.TempleRoom) continue;
			WorldCompilerRect b = room.Bounds;
			altarInTemple |= altarX >= b.X && altarX < b.Right &&
				altarY >= b.Y && altarY < b.Bottom;
			// RasterRoom authors a three-tile roof. Search its actual bricks,
			// never the connected access shaft or an interior floor.
			for (int y = b.Y; y < Math.Min(b.Y + 3, b.Bottom); y++)
			{
				int start = -1;
				for (int x = b.X; x <= b.Right; x++)
				{
					if (x < b.Right && isBrick(x, y))
					{
						if (start < 0) start = x;
						continue;
					}
					int length = start < 0 ? 0 : x - start;
					if (length >= 4 && (y < bestY ||
						(y == bestY && length > bestLength)))
						(bestX, bestY, bestLength) = (start + length / 2, y, length);
					start = -1;
				}
			}
		}
		if (!altarInTemple)
			throw new InvalidOperationException("SOTS Topaz altar is outside the compiler temple rooms.");
		if (bestX < 0)
			throw new InvalidOperationException("SOTS Topaz compiler temple roofs have no four actual adjacent Lihzahrd bricks.");
		return (bestX, bestY);
	}
}
