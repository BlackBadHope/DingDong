using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerAltarContract
{
	internal static bool Valid(WorldCompilerRect actual, int anchorX, int anchorY,
		Func<int, int, bool> altarTile)
	{
		if (actual.X <= 0 || actual.Y <= 0 || actual.Width != 3 || actual.Height != 2 ||
			anchorX != actual.X || anchorY != actual.Y) return false;
		for (int x = actual.X; x < actual.Right; x++)
		for (int y = actual.Y; y < actual.Bottom; y++)
			if (!altarTile(x, y)) return false;
		return true;
	}
}
