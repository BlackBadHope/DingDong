using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public static partial class CandidateDeploymentSitePlanner
{
	private static bool ContainsWorldCriticalAnchor(Rectangle area)
	{
		if (area.Contains(Main.spawnTileX, Main.spawnTileY))
			return true;
		for (int x = area.Left; x < area.Right; x += 8)
		{
			for (int y = area.Top; y < area.Bottom; y += 8)
			{
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && (Main.tileDungeon[tile.TileType] || tile.TileType is TileID.LihzahrdBrick or TileID.LihzahrdAltar))
					return true;
			}
		}

		return false;
	}

	private static bool ContainsWorldCriticalAnchor(Rectangle scanArea, Rectangle assetArea, int blendMargin, AuthoredSceneMutationMask mutationMask)
	{
		if (scanArea.Contains(Main.spawnTileX, Main.spawnTileY) && MutationMayTouchAnchor(Main.spawnTileX, Main.spawnTileY, assetArea, blendMargin, mutationMask))
		{
			return true;
		}

		Rectangle mutationReach = mutationMask.ProtectedFootprint(assetArea.Location, Math.Max(0, blendMargin));
		Rectangle inspect = Rectangle.Intersect(scanArea, mutationReach);
		for (int x = inspect.Left; x < inspect.Right; x++)
		{
			for (int y = inspect.Top; y < inspect.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile || (!Main.tileDungeon[tile.TileType] && tile.TileType is not TileID.LihzahrdBrick and not TileID.LihzahrdAltar))
				{
					continue;
				}

				if (MutationMayTouchAnchor(x, y, assetArea, blendMargin, mutationMask))
					return true;
			}
		}

		return false;
	}
}
