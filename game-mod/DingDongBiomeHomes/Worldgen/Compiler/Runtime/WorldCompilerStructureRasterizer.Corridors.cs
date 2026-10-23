using System;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerStructureRasterizer
{
	private static long RasterCorridor(WorldCompilerPlan plan,
		WorldCompilerChunk chunk, WorldCompilerCorridor corridor, bool opening)
	{
		WorldCompilerRoom from = plan.Structures.Rooms[corridor.From];
		WorldCompilerRoom to = plan.Structures.Rooms[corridor.To];
		if (from.Kind != to.Kind || from.Kind is not
			(WorldCompilerStructureKind.DungeonRoom or
			WorldCompilerStructureKind.TempleRoom)) return 0;
		int fromX = from.Bounds.X + from.Bounds.Width / 2;
		int fromY = from.Bounds.Y + from.Bounds.Height / 2;
		int toX = to.Bounds.X + to.Bounds.Width / 2;
		int toY = to.Bounds.Y + to.Bounds.Height / 2;
		(ushort tileType, ushort wallType) = Materials(from.Kind);
		long changed = RasterHorizontal(chunk, fromX, toX, fromY,
			corridor.Width, tileType, wallType, opening);
		changed += RasterVertical(chunk, toX, fromY, toY, corridor.Width,
			tileType, wallType, opening);
		return changed;
	}

	private static long RasterHorizontal(WorldCompilerChunk chunk, int fromX,
		int toX, int centerY, int width, ushort tileType, ushort wallType, bool opening)
	{
		int half = Math.Max(2, width / 2);
		int cap = opening ? half - 1 : 0;
		int left = Math.Max(chunk.Left, Math.Min(fromX, toX) - cap);
		int right = Math.Min(chunk.Right - 1, Math.Max(fromX, toX) + cap);
		long changed = 0;
		for (int x = left; x <= right; x++)
		for (int y = centerY - half; y <= centerY + half; y++)
			changed += WriteCorridor(x, y, y == centerY - half ||
				y == centerY + half, tileType, wallType, opening);
		return changed;
	}

	private static long RasterVertical(WorldCompilerChunk chunk, int centerX,
		int fromY, int toY, int width, ushort tileType, ushort wallType, bool opening)
	{
		int half = Math.Max(2, width / 2);
		int left = Math.Max(chunk.Left, centerX - half);
		int right = Math.Min(chunk.Right - 1, centerX + half);
		int cap = opening ? half - 1 : 0;
		long changed = 0;
		for (int x = left; x <= right; x++)
		for (int y = Math.Min(fromY, toY) - cap; y <= Math.Max(fromY, toY) + cap; y++)
			changed += WriteCorridor(x, y, x == centerX - half ||
				x == centerX + half, tileType, wallType, opening);
		return changed;
	}

	private static int WriteCorridor(int x, int y, bool border, ushort tileType,
		ushort wallType, bool opening)
	{
		if (opening && border) return 0;
		Tile tile = Main.tile[x, y];
		tile.ClearEverything();
		tile.WallType = wallType;
		if (border) { tile.HasTile = true; tile.TileType = tileType; }
		return 1;
	}
}
