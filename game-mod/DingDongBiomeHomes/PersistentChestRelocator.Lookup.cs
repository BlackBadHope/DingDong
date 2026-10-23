using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes;
/// <summary>
/// Lossless chest relocation for dedicated probes and authored worldgen. The
/// existing chest index is rebound to a verified destination before source
/// tiles are cleared, so full modded chest arrays remain safe.
/// </summary>
public static partial class PersistentChestRelocator
{
	private static bool DestinationTilesMatch(ChestRelocationPlan plan)
	{
		for (int x = 0; x < 2; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				Tile tile = Main.tile[plan.DestinationTopLeft.X + x, plan.DestinationTopLeft.Y + y];
				if (!tile.HasTile || tile.TileType != plan.TileType || tile.TileFrameX != plan.Style * 36 + x * 18 || tile.TileFrameY != y * 18)
					return false;
			}
		}

		return true;
	}

	private static bool TryFindDestination(Rectangle protectedArea, Point16 source, IReadOnlyList<Rectangle> forbiddenAreas, out Point16 destination)
	{
		for (int horizontal = ObjectClearance; horizontal <= MaxHorizontalSearch; horizontal++)
		{
			int leftX = protectedArea.Left - 2 - horizontal;
			int rightX = protectedArea.Right + horizontal;
			for (int vertical = 0; vertical <= MaxVerticalSearch; vertical++)
			{
				int firstY = source.Y + vertical;
				if (CanPlaceChestAt(leftX, firstY, protectedArea, forbiddenAreas))
				{
					destination = new Point16(leftX, firstY);
					return true;
				}

				if (CanPlaceChestAt(rightX, firstY, protectedArea, forbiddenAreas))
				{
					destination = new Point16(rightX, firstY);
					return true;
				}

				if (vertical == 0)
					continue;
				int secondY = source.Y - vertical;
				if (CanPlaceChestAt(leftX, secondY, protectedArea, forbiddenAreas))
				{
					destination = new Point16(leftX, secondY);
					return true;
				}

				if (CanPlaceChestAt(rightX, secondY, protectedArea, forbiddenAreas))
				{
					destination = new Point16(rightX, secondY);
					return true;
				}
			}
		}

		destination = default;
		return false;
	}
}
