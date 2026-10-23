using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerTerrainRasterizer
{
	private static long RasterSolidColumn(int height, int x, int ground,
		WorldCompilerBiome biome)
	{
		int underworld = height - Math.Max(320, height / 14);
		int cursor = ground;
		switch (biome)
		{
			case WorldCompilerBiome.Sahara:
				cursor = FillDepth(x, ground, cursor,
					Math.Max(90, height / 48), underworld, TileID.Sand);
				cursor = FillDepth(x, ground, cursor,
					Math.Max(220, height / 20), underworld, TileID.HardenedSand,
					WallID.HardenedSand);
				cursor = FillDepth(x, ground, cursor,
					Math.Max(420, height / 10), underworld, TileID.Sandstone,
					WallID.Sandstone);
				break;
			case WorldCompilerBiome.Arctic:
				cursor = FillDepth(x, ground, cursor,
					Math.Max(180, height / 24), underworld, TileID.SnowBlock);
				cursor = FillDepth(x, ground, cursor,
					Math.Max(420, height / 11), underworld, TileID.IceBlock);
				break;
			case WorldCompilerBiome.Amazon:
				cursor = FillRange(x, cursor, Math.Min(cursor + 1, underworld),
					TileID.JungleGrass);
				cursor = FillDepth(x, ground, cursor,
					Math.Max(480, height / 9), underworld, TileID.Mud);
				break;
			case WorldCompilerBiome.Ocean:
				cursor = FillDepth(x, ground, cursor,
					Math.Max(180, height / 22), underworld, TileID.Sand);
				break;
			default:
				cursor = FillDepth(x, ground, cursor,
					Math.Max(260, height / 18), underworld, TileID.Dirt);
				break;
		}
		cursor = FillRange(x, cursor, underworld, TileID.Stone);
		FillRange(x, cursor, height, TileID.Ash);
		return height - ground;
	}

	private static int FillDepth(int x, int ground, int cursor,
		int depthExclusive, int limit, ushort tileType) =>
		FillRange(x, cursor, Math.Min(ground + depthExclusive, limit), tileType);

	private static int FillDepth(int x, int ground, int cursor,
		int depthExclusive, int limit, ushort tileType, ushort wallType) =>
		FillRange(x, cursor, Math.Min(ground + depthExclusive, limit), tileType,
			wallType);

	private static int FillRange(int x, int start, int end, ushort tileType)
		=> FillRange(x, start, end, tileType, wallType: 0);

	private static int FillRange(int x, int start, int end, ushort tileType,
		ushort wallType)
	{
		for (int y = start; y < end; y++)
		{
			Tile tile = Main.tile[x, y];
			tile.HasTile = true;
			tile.TileType = tileType;
			if (wallType != 0) tile.WallType = wallType;
		}
		return end;
	}
}
