using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerCandidateLattice
{
	internal static WorldCompilerPoint[] Create(WorldCompilerRect region,
		int spacingX, int spacingY, ulong seed)
	{
		if (spacingX <= 0 || spacingY <= 0)
			throw new ArgumentOutOfRangeException(nameof(spacingX));
		var result = new List<WorldCompilerPoint>();
		int row = 0;
		for (int baseY = region.Y + spacingY / 2; baseY < region.Bottom;
			baseY += spacingY, row++)
		{
			int column = 0;
			for (int baseX = region.X + spacingX / 2; baseX < region.Right;
				baseX += spacingX, column++)
			{
				int key = checked(row * 65_537 + column);
				int jitterX = WorldCompilerSeed.Range(seed, key, 31,
					-spacingX / 4, spacingX / 4 + 1);
				int jitterY = WorldCompilerSeed.Range(seed, key, 37,
					-spacingY / 4, spacingY / 4 + 1);
				int x = Math.Clamp(baseX + jitterX, region.X, region.Right - 1);
				int y = Math.Clamp(baseY + jitterY, region.Y, region.Bottom - 1);
				result.Add(new WorldCompilerPoint(x, y));
			}
		}
		return result.ToArray();
	}
}
