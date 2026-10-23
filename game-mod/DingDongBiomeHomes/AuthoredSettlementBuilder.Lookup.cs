using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;
public static partial class AuthoredSettlementBuilder
{
	public static bool TryFindHousingRoom(Rectangle area, int npcType, out HousingRoomInfo room, out string failureReason)
	{
		foreach (HousingRoomInfo candidate in FindHousingRooms(area, 64))
		{
			if (ValidateHousing(candidate.CheckPoint, npcType, out _))
			{
				room = candidate;
				failureReason = string.Empty;
				return true;
			}
		}

		room = default;
		failureReason = $"No valid housing room for npcType={npcType} inside {area}.";
		return false;
	}

	private static List<HousingRoomInfo> FindHousingRooms(Rectangle area, int limit)
	{
		List<HousingRoomInfo> rooms = new();
		HashSet<string> seen = new(StringComparer.Ordinal);
		for (int y = area.Top + 1; y < area.Bottom - 1 && rooms.Count < limit; y += 2)
		{
			for (int x = area.Left + 1; x < area.Right - 1 && rooms.Count < limit; x += 2)
			{
				if (!WorldGen.InWorld(x, y, 12) || WorldGen.SolidTile(x, y))
					continue;
				Tile tile = Main.tile[x, y];
				if (tile.WallType == WallID.None || tile.WallType >= Main.wallHouse.Length || !Main.wallHouse[tile.WallType])
					continue;
				bool alreadyCovered = false;
				foreach (HousingRoomInfo room in rooms)
				{
					if (room.Bounds.Contains(x, y))
					{
						alreadyCovered = true;
						break;
					}
				}

				if (alreadyCovered || !ValidateHousing(new Point(x, y), NPCID.Guide, out _))
					continue;
				Rectangle bounds = new(WorldGen.roomX1, WorldGen.roomY1, WorldGen.roomX2 - WorldGen.roomX1 + 1, WorldGen.roomY2 - WorldGen.roomY1 + 1);
				if (!area.Contains(WorldGen.bestX, WorldGen.bestY))
					continue;
				string key = $"{bounds.X}:{bounds.Y}:{bounds.Width}:{bounds.Height}";
				if (!seen.Add(key))
					continue;
				rooms.Add(new HousingRoomInfo(bounds, new Point(x, y), new Point(WorldGen.bestX, WorldGen.bestY)));
			}
		}

		return rooms;
	}
}
