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
	private static int FindSurfaceFloor(int x)
	{
		int minY = Math.Max(30, Math.Min(Main.spawnTileY - 100, (int)Main.worldSurface - 180));
		int maxY = Math.Min(Main.maxTilesY - 40, Math.Max(Main.spawnTileY + 180, (int)Main.worldSurface + 300));
		for (int y = minY; y <= maxY; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
				return y;
		}

		throw new InvalidOperationException($"DingDong could not find a solid spawn surface at x={x}; scan={minY}..{maxY}.");
	}
}
