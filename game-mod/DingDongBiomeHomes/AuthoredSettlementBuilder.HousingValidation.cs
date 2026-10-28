using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;
public static partial class AuthoredSettlementBuilder
{
	internal static bool TryValidateOriginPoint(Point point, int npcType, out Point home)
	{
		if (ValidateHousing(point, npcType, out _))
		{
			home = new Point(WorldGen.bestX, WorldGen.bestY);
			return true;
		}
		home = default;
		return false;
	}

	internal static IReadOnlyDictionary<string, int> ExplainHousingFailures(Rectangle area)
	{
		Dictionary<string, int> failures = new(StringComparer.Ordinal);
		for (int y = area.Top + 1; y < area.Bottom - 1; y += 2)
		for (int x = area.Left + 1; x < area.Right - 1; x += 2)
		{
			if (!WorldGen.InWorld(x, y, 12) || WorldGen.SolidTile(x, y)) continue;
			Tile tile = Main.tile[x, y];
			if (tile.WallType == 0 || tile.WallType >= Main.wallHouse.Length || !Main.wallHouse[tile.WallType]) continue;
			if (ValidateHousing(new Point(x, y), NPCID.Guide, out string reason)) reason = "PASS";
			failures.TryGetValue(reason, out int count);
			failures[reason] = count + 1;
		}
		return failures;
	}

	public static bool TryValidateHousingAt(Point point, int npcType, out HousingRoomInfo room, out string failureReason)
	{
		for (int radius = 0; radius <= 5; radius++)
		{
			for (int y = point.Y - radius; y <= point.Y + radius; y++)
			{
				for (int x = point.X - radius; x <= point.X + radius; x++)
				{
					if (!WorldGen.InWorld(x, y, 12) || WorldGen.SolidTile(x, y))
						continue;
					Tile tile = Main.tile[x, y];
					if (tile.WallType == WallID.None || tile.WallType >= Main.wallHouse.Length || !Main.wallHouse[tile.WallType])
						continue;
					if (!ValidateHousing(new Point(x, y), npcType, out failureReason))
						continue;
					Rectangle bounds = new(WorldGen.roomX1, WorldGen.roomY1, WorldGen.roomX2 - WorldGen.roomX1 + 1, WorldGen.roomY2 - WorldGen.roomY1 + 1);
					room = new HousingRoomInfo(bounds, new Point(x, y), new Point(WorldGen.bestX, WorldGen.bestY));
					failureReason = string.Empty;
					return true;
				}
			}
		}

		room = default;
		failureReason = $"No valid housing room near {point.X},{point.Y}.";
		return false;
	}

	private static string DescribeOccupant(int ignore)
	{
		for (int i = 0; i < Main.maxNPCs; i++)
		{
			NPC npc = Main.npc[i];
			if (i == ignore || !npc.active || !npc.townNPC || npc.homeless) continue;
			if (npc.homeTileX >= WorldGen.roomX1 && npc.homeTileX <= WorldGen.roomX2 && npc.homeTileY >= WorldGen.roomY1 && npc.homeTileY <= WorldGen.roomY2 + 1)
				return $", occupant={npc.TypeName}#{i}@{npc.homeTileX},{npc.homeTileY}";
		}
		return string.Empty;
	}

	private static bool ValidateHousing(Point point, int npcType, out string failureReason)
	{
		try
		{
			if (!WorldGen.StartRoomCheck(point.X, point.Y))
			{
				failureReason = $"StartRoomCheck={WorldGen.roomCheckFailureReason}; tiles={WorldGen.numRoomTiles}";
				return false;
			}

			if (!WorldGen.RoomNeeds(npcType))
			{
				failureReason = "RoomNeeds=false";
				return false;
			}

			// Vanilla scores a room as -1 when a town NPC already lives in it. After a
			// world load the resident itself may already occupy its own residence, so
			// the post-settling acceptance must ignore that NPC, as the housing UI does.
			int ignore = -1;
			for (int i = 0; i < Main.maxNPCs; i++)
				if (Main.npc[i].active && Main.npc[i].townNPC && Main.npc[i].type == npcType) { ignore = i; break; }
			WorldGen.ScoreRoom(ignore, npcType);
			bool valid = WorldGen.canSpawn && WorldGen.hiScore > 0;
			failureReason = valid ? string.Empty : $"ScoreRoom canSpawn={WorldGen.canSpawn}, hiScore={WorldGen.hiScore}{DescribeOccupant(ignore)}";
			return valid;
		}
		catch (Exception exception)
		{
			failureReason = $"{exception.GetType().Name}: {exception.Message}";
			return false;
		}
	}
}
