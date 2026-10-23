using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalDungeonAnnexBuilder
{
	private const int Shell = 3;

	internal static void Build(ContinentalDungeonPlan plan, ContinentalDungeonStyle style)
	{
		BuildRoom(plan.Room, style);
		BuildConnector(plan.Connector, style);
		if (plan.HasPortal)
			BuildPortal(plan.Portal, style);
		BuildPillars(plan.Room, style.Brick, plan.Connector.Center.Y);
		BuildExitSteps(plan.Room, plan.Connector, style.Brick);
		Frame(plan.ProtectedArea);
	}

	private static void BuildPortal(Rectangle portal, ContinentalDungeonStyle style)
	{
		for (int x = portal.Left; x < portal.Right; x++)
			for (int y = portal.Top; y < portal.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearTile();
				tile.LiquidAmount = 0;
				tile.WallType = style.Wall;
			}
	}

	private static void BuildRoom(Rectangle room, ContinentalDungeonStyle style)
	{
		for (int x = room.Left; x < room.Right; x++)
			for (int y = room.Top; y < room.Bottom; y++)
			{
				bool shell = x < room.Left + Shell || x >= room.Right - Shell ||
					y < room.Top + Shell || y >= room.Bottom - Shell;
				Tile tile = Main.tile[x, y];
				tile.ClearTile();
				tile.LiquidAmount = 0;
				tile.WallType = style.Wall;
				if (!shell) continue;
				tile.HasTile = true;
				tile.TileType = WorldGen.genRand.Next(11) == 0 ?
					style.CrackedBrick : style.Brick;
			}
	}

	private static void BuildConnector(Rectangle corridor, ContinentalDungeonStyle style)
	{
		for (int x = corridor.Left; x < corridor.Right; x++)
			for (int y = corridor.Top; y < corridor.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearTile();
				tile.LiquidAmount = 0;
				tile.WallType = style.Wall;
				if (y == corridor.Top || y == corridor.Bottom - 1)
				{
					tile.HasTile = true;
					tile.TileType = style.Brick;
				}
			}
	}

	private static void BuildPillars(Rectangle room, ushort brick, int entryY)
	{
		int[] columns = { room.Left + room.Width / 3, room.Left + room.Width * 2 / 3 };
		foreach (int x in columns)
			for (int y = room.Top + Shell; y < room.Bottom - Shell; y++)
			{
				if (y >= entryY - 3 && y <= entryY + 3) continue;
				Tile tile = Main.tile[x, y];
				tile.ClearTile();
				tile.HasTile = true;
				tile.TileType = brick;
				tile.LiquidAmount = 0;
			}
	}

	private static void BuildExitSteps(Rectangle room, Rectangle connector, ushort brick)
	{
		bool entryOnLeft = connector.Center.X < room.Center.X;
		int startX = entryOnLeft ? room.Left + Shell : room.Right - Shell - 7;
		for (int y = room.Bottom - Shell - 4; y > connector.Center.Y + 3; y -= 4)
			for (int x = startX; x < startX + 7; x++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearTile();
				tile.HasTile = true;
				tile.TileType = brick;
				tile.LiquidAmount = 0;
			}
	}

	private static void Frame(Rectangle area)
	{
		for (int x = area.Left; x < area.Right; x++)
			for (int y = area.Top; y < area.Bottom; y++)
			{
				WorldGen.SquareTileFrame(x, y, true);
				WorldGen.SquareWallFrame(x, y, true);
			}
	}
}
