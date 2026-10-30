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
	private static bool CanPlaceChestAt(int topLeftX, int topLeftY, Rectangle protectedArea, IReadOnlyList<Rectangle> forbiddenAreas)
	{
		Rectangle chestArea = new(topLeftX, topLeftY, 2, 2);
		Rectangle clearance = new(topLeftX - ObjectClearance, topLeftY - ObjectClearance, 2 + ObjectClearance * 2, 2 + ObjectClearance * 2);
		if (chestArea.Intersects(protectedArea) || topLeftX < 40 || topLeftY < 40 || topLeftX + 1 >= Main.maxTilesX - 40 || topLeftY + 2 >= Main.maxTilesY - 40)
			return false;
		// 30.10: the cheap tile test runs before the region, chest, entity and sign scans, so a
		// wide search over open water or solid rock costs almost nothing. Same verdicts as before.
		for (int x = topLeftX; x <= topLeftX + 1; x++)
		{
			for (int y = topLeftY; y <= topLeftY + 1; y++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.HasTile || tile.LiquidAmount > 0)
					return false;
			}

			Tile support = Main.tile[x, topLeftY + 2];
			if (!support.HasTile || !Main.tileSolid[support.TileType] || Main.tileSolidTop[support.TileType])
				return false;
		}
		if (Chest.NearOtherChests(topLeftX, topLeftY))
			return false;
		foreach (Rectangle forbidden in forbiddenAreas)
		{
			if (clearance.Intersects(forbidden))
				return false;
		}

		foreach (Chest chest in Main.chest)
		{
			if (chest != null && clearance.Contains(chest.x, chest.y))
				return false;
		}

		foreach (Point16 position in TileEntity.ByPosition.Keys)
		{
			if (clearance.Contains(position.X, position.Y))
				return false;
		}

		foreach (Sign sign in Main.sign)
		{
			if (sign != null && clearance.Contains(sign.x, sign.y))
				return false;
		}

		return true;
	}
}
