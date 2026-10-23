using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerAccessRasterizer
{
	internal static void Generate(WorldCompilerPlan plan)
	{
		foreach (WorldCompilerCorridor corridor in plan.Structures.Corridors)
		{
			WorldCompilerRoom from = plan.Structures.Rooms[corridor.From];
			WorldCompilerRoom to = plan.Structures.Rooms[corridor.To];
			if (from.CenterY == to.CenterY) continue;
			for (int y = Math.Min(from.CenterY, to.CenterY); y <= Math.Max(from.CenterY, to.CenterY); y++)
			{
				Tile tile = Main.tile[to.CenterX, y];
				if (tile.HasTile && tile.TileType != TileID.Rope)
					throw new InvalidOperationException("Structure climbing shaft was sealed.");
				tile.HasTile = true; tile.TileType = TileID.Rope;
			}
		}
		foreach (WorldCompilerAccess access in WorldCompilerAccessPlan.Create(plan.Surface, plan.Structures.Rooms))
		{
			WorldCompilerRoom room = plan.Structures.Rooms[access.RoomId];
			ushort material = access.Locked ? TileID.LihzahrdBrick : TileID.BlueDungeonBrick;
			ushort wall = access.Locked ? WallID.None : WallID.BlueDungeonUnsafe;
			Write(access.Shaft, material, wall, shell: true);
			Write(new(access.X - 5, access.Bottom - 3,
				room.Bounds.X + 8 - access.X, 7), material, wall, shell: true);
			Write(new(access.X - 3, access.Top, 7, access.Bottom - access.Top + 2), material, wall, false);
			Write(new(access.X - 3, access.Bottom - 1, access.TargetX - access.X + 7, 3), material, wall, false);
			for (int y = access.Top; y <= access.Bottom; y++)
			{
				Tile tile = Main.tile[access.X, y];
				tile.HasTile = true;
				tile.TileType = TileID.Rope;
			}
			// A visible landing, with a shaft opening and a safe ledge for the Old Man.
			Write(new(access.X - 12, access.Top - 1, 30, 8), material, WallID.None, false);
			for (int x = access.X - 12; x < access.X + 18; x++)
				if (Math.Abs(x - access.X) > 3) Solid(x, access.Top + 7, material);
			foreach (var crossing in WorldCompilerAccessCrossings.Openings(access.Shaft,
				access.Locked, plan.Structures.Rooms, plan.Structures.Corridors))
			for (int x = crossing.X; x < crossing.Right; x++)
			for (int y = crossing.Y; y < crossing.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				// Keep the rope and background wall. Only this shaft's masonry is reopened.
				if (tile.HasTile && tile.TileType == material) tile.ClearTile();
			}
			Protect(access.Shaft, 18);
			Protect(access.Hall, 18);
			Protect(new(access.X - 12, access.Top - 1, 30, 10), 8);
		}
	}

	private static void Write(WorldCompilerRect area, ushort material, ushort wall, bool shell)
	{
		for (int x = area.X; x < area.Right; x++)
		for (int y = area.Y; y < area.Bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			tile.ClearEverything();
			tile.WallType = wall;
			if (shell) { tile.HasTile = true; tile.TileType = material; }
		}
	}

	private static void Solid(int x, int y, ushort type)
	{
		Tile tile = Main.tile[x, y];
		tile.ClearEverything();
		tile.HasTile = true;
		tile.TileType = type;
	}

	private static void Protect(WorldCompilerRect area, int padding) =>
		GenVars.structures.AddProtectedStructure(new Rectangle(area.X, area.Y,
			area.Width, area.Height), padding);
}
