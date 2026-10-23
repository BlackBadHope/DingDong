using System;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerProgressionObjects
{
	internal static WorldCompilerProgressionReceipt Place(WorldCompilerPlan plan)
	{
		var receipt = new WorldCompilerProgressionReceipt();
		WorldCompilerRoom lastTemple = default;
		foreach (WorldCompilerRoom room in plan.Structures.Rooms)
		{
			switch (room.Kind)
			{
				case WorldCompilerStructureKind.TempleRoom: lastTemple = room; break;
				case WorldCompilerStructureKind.HiveChamber:
					PlaceTile(room, TileID.Larva, 3, 3, 0, "queen-bee-larva", receipt);
					FillHoney(room); break;
				case WorldCompilerStructureKind.EvilShrine:
					PlaceTile(room, TileID.DemonAltar, 3, 2, WorldGen.crimson ? 1 : 0,
						"evil-altar", receipt);
					PlaceTile(room, TileID.ShadowOrbs, 2, 2, WorldGen.crimson ? 1 : 0,
						"evil-orb", receipt, offset: 12); break;
				case WorldCompilerStructureKind.UnderworldForge:
					PlaceTile(room, TileID.Hellforge, 3, 2, 0, "hellforge", receipt); break;
			}
		}
		if (lastTemple.Kind != WorldCompilerStructureKind.TempleRoom)
			throw new InvalidOperationException("No chamber for the Golem altar.");
		WorldCompilerRect altar = PlaceTile(lastTemple, TileID.LihzahrdAltar, 3, 2,
			0, "golem-altar", receipt);
		GenVars.lAltarX = altar.X;
		GenVars.lAltarY = altar.Y;
		PlaceTempleDoor(plan, receipt);
		WorldCompilerProgressionChests.Place(plan, receipt);
		return receipt;
	}

	private static WorldCompilerRect PlaceTile(WorldCompilerRoom room, int type,
		int width, int height, int style, string name,
		WorldCompilerProgressionReceipt receipt, int offset = 6)
	{
		int x = room.Bounds.X + offset;
		int y = room.Bounds.Bottom - 4;
		// PlaceTile has no native multi-cell dispatch for shadow orbs/hearts.
		if (type == TileID.ShadowOrbs) WorldGen.AddShadowOrb(x, y);
		else WorldGen.PlaceTile(x, y, type, mute: true, forced: false, style: style);
		int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0, cells = 0;
		for (int tx = x - width; tx <= x + width; tx++)
		for (int ty = y - height; ty <= y + height; ty++)
			if (Main.tile[tx, ty].HasTile && Main.tile[tx, ty].TileType == type)
			{
				minX = Math.Min(minX, tx); minY = Math.Min(minY, ty);
				maxX = Math.Max(maxX, tx); maxY = Math.Max(maxY, ty); cells++;
			}
		if (cells != width * height)
			throw new InvalidOperationException($"Progression placement failed: {name} at {x},{y}; cells={cells}.");
		var bounds = new WorldCompilerRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
		receipt.Objects.Add(new(name, type, style, bounds, width * height));
		return bounds;
	}

	private static void FillHoney(WorldCompilerRoom room)
	{
		for (int x = room.Bounds.X + 14; x < room.Bounds.Right - 4; x++)
		for (int y = room.Bounds.Bottom - 6; y < room.Bounds.Bottom - 3; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.HasTile) throw new InvalidOperationException("Hive honey basin is obstructed.");
			tile.LiquidType = LiquidID.Honey;
			tile.LiquidAmount = 255;
		}
	}

	private static void PlaceTempleDoor(WorldCompilerPlan plan,
		WorldCompilerProgressionReceipt receipt)
	{
		foreach (WorldCompilerAccess access in WorldCompilerAccessPlan.Create(plan.Surface, plan.Structures.Rooms))
		{
			if (!access.Locked) continue;
			int x = plan.Structures.Rooms[access.RoomId].Bounds.X + 1;
			// Native locked Lihzahrd door: WorldGen.IsLockedDoor checks frames 594..646 (style 11).
			WorldGen.PlaceTile(x, access.Bottom + 1, TileID.ClosedDoor, true, false, -1, 11);
			receipt.Objects.Add(new("temple-locked-door", TileID.ClosedDoor, 11,
				new(x, access.Bottom - 1, 1, 3), 3));
		}
	}
}
