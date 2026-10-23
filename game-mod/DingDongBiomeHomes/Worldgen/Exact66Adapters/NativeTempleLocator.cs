using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class NativeTempleLocator
{
	internal static (int X, int Y) FindBrick()
	{
		if (TryNear(GenVars.lAltarX, GenVars.lAltarY, out var seed)) return seed;
		if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
		{
			object value = calamity.Code.GetType("CalamityMod.World.CustomTemple")?
				.GetField("NewAlterPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
			if (value is Point point && TryNear(point.X, point.Y, out seed)) return seed;
		}
		if (GenVars.tLeft > 1 && GenVars.tRight < Main.maxTilesX - 1 &&
			GenVars.tTop > 1 && GenVars.tBottom < Main.maxTilesY - 1 &&
			GenVars.tLeft < GenVars.tRight && GenVars.tTop < GenVars.tBottom)
		{
			for (int y = GenVars.tTop; y <= GenVars.tBottom; y++)
			for (int x = GenVars.tLeft; x <= GenVars.tRight; x++)
				if (Brick(x, y)) return (x, y);
		}
		// Save/load need not retain generator globals. Locate a real altar, not a synthetic coordinate.
		for (int x = 100; x < Main.maxTilesX - 100; x++)
		for (int y = 100; y < Main.maxTilesY - 100; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.HasTile && tile.TileType == TileID.LihzahrdAltar && TryNear(x, y, out seed)) return seed;
		}
		throw new InvalidOperationException("No physical Lihzahrd temple found via vanilla altar, Calamity altar, temple bounds or saved altar tiles.");
	}

	private static bool TryNear(int x, int y, out (int X, int Y) seed)
	{
		seed = default;
		if (!WorldGen.InWorld(x, y, 65)) return false;
		for (int radius = 0; radius <= 64; radius++)
		{
			for (int i = x - radius; i <= x + radius; i++)
			{
				if (Brick(i, y - radius)) { seed = (i, y - radius); return true; }
				if (Brick(i, y + radius)) { seed = (i, y + radius); return true; }
			}
			for (int j = y - radius + 1; j < y + radius; j++)
			{
				if (Brick(x - radius, j)) { seed = (x - radius, j); return true; }
				if (Brick(x + radius, j)) { seed = (x + radius, j); return true; }
			}
		}
		return false;
	}

	private static bool Brick(int x, int y) => Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.LihzahrdBrick;
}
