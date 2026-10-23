using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerDecorationRasterizer
{
	private static long MushroomCavern(WorldCompilerPlan plan,
		WorldCompilerDecoration entry)
	{
		long changed = 0;
		int radiusX = entry.Width / 2;
		int radiusY = entry.Height / 2;
		int centerY = entry.Y - radiusY;
		long limit = (long)radiusX * radiusX * radiusY * radiusY;
		for (int x = entry.X - radiusX; x <= entry.X + radiusX; x++)
		for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
		{
			if (x < 1 || x >= plan.Width - 1 || y < 1 || y >= plan.Height - 1)
				continue;
			long dx = x - entry.X;
			long dy = y - centerY;
			if (dx * dx * radiusY * radiusY + dy * dy * radiusX * radiusX > limit)
				continue;
			Tile tile = Main.tile[x, y];
			tile.ClearEverything();
			tile.WallType = WallID.MushroomUnsafe;
			if (y >= centerY + radiusY - 4)
			{
				tile.HasTile = true;
				tile.TileType = TileID.MushroomGrass;
			}
			changed++;
		}
		return changed;
	}
}
