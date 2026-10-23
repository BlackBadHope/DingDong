using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerStructureRasterizer
{
	private static long RasterResidenceCavern(WorldCompilerChunk chunk,
		WorldCompilerRect bounds, bool mushroom)
	{
		long changed = 0;
		int centerX = bounds.X + bounds.Width / 2;
		int centerY = bounds.Y + bounds.Height / 2;
		int radiusX = bounds.Width / 2;
		int radiusY = bounds.Height / 2;
		long limit = (long)radiusX * radiusX * radiusY * radiusY;
		int left = Math.Max(chunk.Left, bounds.X);
		int right = Math.Min(chunk.Right, bounds.Right);
		for (int x = left; x < right; x++)
		for (int y = bounds.Y; y < bounds.Bottom; y++)
		{
			long dx = x - centerX;
			long dy = y - centerY;
			bool inside = dx * dx * radiusY * radiusY +
				dy * dy * radiusX * radiusX <= limit;
			Tile tile = Main.tile[x, y];
			tile.ClearEverything();
			tile.WallType = mushroom ? WallID.MushroomUnsafe : WallID.CaveUnsafe;
			if (!inside ||
				y >= bounds.Bottom - WorldCompilerResidenceCavernContract.FloorDepth)
			{
				tile.HasTile = true;
				tile.TileType = mushroom ? TileID.MushroomGrass : TileID.Stone;
			}
			changed++;
		}
		return changed;
	}
}
