using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerStructureRasterizer
{
	private static long RasterIsland(WorldCompilerChunk chunk,
		WorldCompilerRect bounds)
	{
		int centerX = bounds.X + bounds.Width / 2;
		int centerY = bounds.Y + bounds.Height / 2;
		int radiusX = bounds.Width / 2;
		int radiusY = bounds.Height / 2;
		int left = Math.Max(chunk.Left, bounds.X);
		int right = Math.Min(chunk.Right, bounds.Right);
		long limit = (long)radiusX * radiusX * radiusY * radiusY;
		long changed = 0;
		for (int x = left; x < right; x++)
		{
			bool topWritten = false;
			for (int y = bounds.Y; y < bounds.Bottom; y++)
			{
				long dx = x - centerX;
				long dy = y - centerY;
				if (dx * dx * radiusY * radiusY + dy * dy * radiusX * radiusX > limit)
					continue;
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.HasTile = true;
				tile.TileType = topWritten ? TileID.Dirt : TileID.Grass;
				topWritten = true;
				changed++;
			}
		}
		return changed;
	}

	private static long RasterPyramid(WorldCompilerChunk chunk,
		WorldCompilerRect bounds)
	{
		int centerX = bounds.X + bounds.Width / 2;
		int left = Math.Max(chunk.Left, bounds.X);
		int right = Math.Min(chunk.Right, bounds.Right);
		long changed = 0;
		for (int y = bounds.Y; y < bounds.Bottom; y++)
		{
			int half = Math.Max(2, bounds.Width * (y - bounds.Y + 1) /
				(bounds.Height * 2));
			for (int x = Math.Max(left, centerX - half);
				x < Math.Min(right, centerX + half); x++)
			{
				bool shell = Math.Abs(x - centerX) >= half - 3 ||
					y >= bounds.Bottom - 4;
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.WallType = WallID.SandstoneBrick;
				if (shell) { tile.HasTile = true; tile.TileType = TileID.SandstoneBrick; }
				changed++;
			}
		}
		return changed;
	}
}
