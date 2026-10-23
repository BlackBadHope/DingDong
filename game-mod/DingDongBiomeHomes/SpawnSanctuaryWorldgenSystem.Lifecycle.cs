using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
/// <summary>
/// Adds one seed-selected authored Structure Helper house to the world spawn.
/// A compact stone foundation, low lava sills and a final PostWorldGen drain
/// keep FTW/other generator passes from leaving the player inside lava without
/// turning the whole visible spawn into an arena-sized black box.
/// </summary>
public sealed partial class SpawnSanctuaryWorldgenSystem
{
	private static bool IsSpawnPocketSafe(int centerX, int floorY)
	{
		for (int x = centerX - SpawnPocketHalfWidth; x <= centerX + SpawnPocketHalfWidth; x++)
		{
			for (int y = floorY - SpawnPocketHeight; y < floorY; y++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.HasTile || tile.LiquidAmount > 0)
					return false;
			}

			Tile floor = Main.tile[x, floorY];
			if (!floor.HasTile || !Main.tileSolid[floor.TileType] || Main.tileSolidTop[floor.TileType])
				return false;
		}

		return true;
	}
}
