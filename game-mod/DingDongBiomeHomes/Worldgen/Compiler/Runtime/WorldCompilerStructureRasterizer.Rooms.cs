using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerStructureRasterizer
{
	private static long RasterRoom(WorldCompilerChunk chunk, WorldCompilerRoom room)
	{
		int left = Math.Max(chunk.Left, room.Bounds.X);
		int right = Math.Min(chunk.Right, room.Bounds.Right);
		(ushort tileType, ushort wallType) = Materials(room.Kind);
		long changed = 0;
		for (int x = left; x < right; x++)
		for (int y = room.Bounds.Y; y < room.Bounds.Bottom; y++)
		{
			bool border = x - room.Bounds.X < 3 || room.Bounds.Right - x <= 3 ||
				y - room.Bounds.Y < 3 || room.Bounds.Bottom - y <= 3;
			Tile tile = Main.tile[x, y];
			tile.ClearEverything();
			tile.WallType = wallType;
			if (border)
			{
				tile.HasTile = true;
				tile.TileType = tileType;
			}
			changed++;
		}
		return changed;
	}

	private static (ushort Tile, ushort Wall) Materials(
		WorldCompilerStructureKind kind) => kind switch
		{
			WorldCompilerStructureKind.DungeonRoom =>
				(TileID.BlueDungeonBrick, WallID.BlueDungeonUnsafe),
			WorldCompilerStructureKind.TempleRoom =>
				(TileID.LihzahrdBrick, WallID.LihzahrdBrickUnsafe),
			WorldCompilerStructureKind.SpawnSanctuary =>
				(TileID.WoodBlock, WallID.Wood),
			WorldCompilerStructureKind.HiveChamber =>
				(TileID.Hive, WallID.HiveUnsafe),
			WorldCompilerStructureKind.UnderworldForge =>
				(TileID.ObsidianBrick, WallID.ObsidianBrickUnsafe),
			WorldCompilerStructureKind.JungleShrine =>
				(TileID.Mudstone, WallID.MudstoneBrick),
			_ => (TileID.Stone, WallID.Stone)
		};

	private static long OpenRoom(WorldCompilerChunk chunk, WorldCompilerRoom room)
	{
		long changed = 0;
		for (int x = Math.Max(chunk.Left, room.Bounds.X + 3);
			x < Math.Min(chunk.Right, room.Bounds.Right - 3); x++)
		for (int y = room.Bounds.Y + 3; y < room.Bounds.Bottom - 3; y++)
		{
			Tile tile = Main.tile[x, y];
			tile.HasTile = false;
			tile.LiquidAmount = 0;
			changed++;
		}
		return changed;
	}
}
