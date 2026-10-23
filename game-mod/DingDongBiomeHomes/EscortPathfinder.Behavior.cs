using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
internal static partial class EscortPathfinder
{
	private static IEnumerable<Point> Neighbors(NPC npc, Point current, Rectangle bounds)
	{
		for (int direction = -1; direction <= 1; direction += 2)
		{
			int x = current.X + direction;
			for (int deltaY = -5; deltaY <= 8; deltaY++)
			{
				Point candidate = new(x, current.Y + deltaY);
				if (bounds.Contains(candidate) && IsStandable(npc, candidate))
				{
					yield return candidate;
					break;
				}
			}

			// Bounded five-tile leap edges bridge small pits without whole-world path searches.
			for (int leap = 2; leap <= 5; leap++)
			{
				for (int deltaY = -5; deltaY <= 1; deltaY++)
				{
					Point candidate = new(current.X + direction * leap, current.Y + deltaY);
					if (!bounds.Contains(candidate) || !IsStandable(npc, candidate) || !ArcClear(npc, current, candidate))
						continue;
					yield return candidate;
					deltaY = 2;
					leap = 6;
				}
			}
		}
	}

	private static bool TryNearestStandable(NPC npc, Point origin, int radius, out Point result)
	{
		for (int distance = 0; distance <= radius; distance++)
		{
			for (int y = origin.Y - distance; y <= origin.Y + distance; y++)
			{
				for (int x = origin.X - distance; x <= origin.X + distance; x++)
				{
					if (Math.Max(Math.Abs(x - origin.X), Math.Abs(y - origin.Y)) != distance)
						continue;
					Point candidate = new(x, y);
					if (IsStandable(npc, candidate))
					{
						result = candidate;
						return true;
					}
				}
			}
		}

		result = default;
		return false;
	}

	private static bool IsStandable(NPC npc, Point foot)
	{
		if (!WorldGen.InWorld(foot.X, foot.Y, 10))
			return false;
		Tile support = Framing.GetTileSafely(foot.X, foot.Y);
		bool supported = support.HasTile && (Main.tileSolid[support.TileType] && !Main.tileSolidTop[support.TileType] || support.TileType < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[support.TileType]);
		if (!supported || support.LiquidAmount > 160 && support.LiquidType == LiquidID.Lava)
			return false;
		Vector2 position = new(foot.X * 16f + 8f - npc.width * 0.5f, foot.Y * 16f - npc.height);
		return !Collision.SolidCollision(position, npc.width, npc.height);
	}

	private static float Heuristic(Point left, Point right) => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y) * 1.15f;
}
