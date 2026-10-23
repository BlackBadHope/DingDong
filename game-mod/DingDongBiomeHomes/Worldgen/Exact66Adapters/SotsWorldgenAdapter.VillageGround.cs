using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenAdapter
{
	private static int FindVillageGround(int x, Rectangle evil)
	{
		ISet<int> grass = (ISet<int>)villageGrassField.GetValue(null);
		ISet<int> stone = (ISet<int>)villageStoneField.GetValue(null);
		int naturalGround = -1;
		for (int y = Math.Max(100, evil.Top); y < evil.Bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			// Native entrance scan continues below unsuitable solids; its relaxed
			// branch accepts evil walls too, but not arbitrary solid tiles.
			bool evilWall = tile.WallType is 83 or 3;
			if ((tile.HasTile && Main.tileSolid[tile.TileType] || evilWall) &&
				(grass.Contains(tile.TileType) || stone.Contains(tile.TileType) || evilWall)) return y;
			// Compiler infection does not convert dirt/mud to evil grass. Keep a
			// natural substrate fallback; the caller still checks all reservations.
			if (naturalGround < 0 && tile.HasTile && Main.tileSolid[tile.TileType] &&
				SotsVillageGroundPolicy.IsNatural(tile.TileType)) naturalGround = y;
		}
		return naturalGround;
	}
}
