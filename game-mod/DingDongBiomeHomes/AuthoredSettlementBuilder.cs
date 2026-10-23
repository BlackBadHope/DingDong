using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;
public static partial class AuthoredSettlementBuilder
{
	public static List<HousingShellProbe> ProbeHousingShells(Rectangle area, int limit = 64)
	{
		HashSet<int> chairs = new(TileID.Sets.RoomNeeds.CountsAsChair);
		HashSet<int> tables = new(TileID.Sets.RoomNeeds.CountsAsTable);
		HashSet<int> torches = new(TileID.Sets.RoomNeeds.CountsAsTorch);
		HashSet<int> doors = new(TileID.Sets.RoomNeeds.CountsAsDoor);
		HashSet<string> seen = new(StringComparer.Ordinal);
		List<HousingShellProbe> shells = new();
		for (int y = area.Top + 1; y < area.Bottom - 1 && shells.Count < limit; y += 2)
		{
			for (int x = area.Left + 1; x < area.Right - 1 && shells.Count < limit; x += 2)
			{
				if (!WorldGen.InWorld(x, y, 12) || WorldGen.SolidTile(x, y))
					continue;
				Tile checkTile = Main.tile[x, y];
				if (checkTile.WallType == WallID.None || checkTile.WallType >= Main.wallHouse.Length || !Main.wallHouse[checkTile.WallType])
					continue;
				if (!WorldGen.StartRoomCheck(x, y))
					continue;
				Rectangle bounds = new(WorldGen.roomX1, WorldGen.roomY1, WorldGen.roomX2 - WorldGen.roomX1 + 1, WorldGen.roomY2 - WorldGen.roomY1 + 1);
				if (!area.Contains(WorldGen.bestX, WorldGen.bestY))
					continue;
				string key = $"{bounds.X}:{bounds.Y}:{bounds.Width}:{bounds.Height}";
				if (!seen.Add(key))
					continue;
				int chairCount = 0;
				int tableCount = 0;
				int torchCount = 0;
				int doorCount = 0;
				int left = Math.Max(area.Left, bounds.Left - 2);
				int right = Math.Min(area.Right - 1, bounds.Right + 2);
				int top = Math.Max(area.Top, bounds.Top - 2);
				int bottom = Math.Min(area.Bottom - 1, bounds.Bottom + 2);
				for (int tileX = left; tileX <= right; tileX++)
				{
					for (int tileY = top; tileY <= bottom; tileY++)
					{
						Tile tile = Main.tile[tileX, tileY];
						if (!tile.HasTile)
							continue;
						if (chairs.Contains(tile.TileType))
							chairCount++;
						if (tables.Contains(tile.TileType))
							tableCount++;
						if (torches.Contains(tile.TileType))
							torchCount++;
						if (doors.Contains(tile.TileType))
							doorCount++;
					}
				}

				bool roomNeeds = WorldGen.RoomNeeds(NPCID.Guide);
				shells.Add(new HousingShellProbe(bounds, new Point(x, y), new Point(WorldGen.bestX, WorldGen.bestY), chairCount, tableCount, torchCount, doorCount, roomNeeds));
			}
		}

		return shells;
	}

	public static HousingProbeResult ProbeHousing(Rectangle area, int limit = 32)
	{
		List<HousingRoomInfo> rooms = FindHousingRooms(area, limit);
		int candidatePoints = 0;
		string firstFailure = string.Empty;
		for (int y = area.Top + 1; y < area.Bottom - 1; y += 2)
		{
			for (int x = area.Left + 1; x < area.Right - 1; x += 2)
			{
				if (!WorldGen.InWorld(x, y, 12) || WorldGen.SolidTile(x, y))
					continue;
				Tile tile = Main.tile[x, y];
				if (tile.WallType == WallID.None || tile.WallType >= Main.wallHouse.Length || !Main.wallHouse[tile.WallType])
					continue;
				candidatePoints++;
				if (firstFailure.Length == 0 && !ValidateHousing(new Point(x, y), NPCID.Guide, out string failure))
					firstFailure = failure;
			}
		}

		return new HousingProbeResult(rooms.Count, candidatePoints, firstFailure);
	}
}
