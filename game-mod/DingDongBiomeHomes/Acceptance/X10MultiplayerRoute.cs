using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Acceptance;

internal static class X10MultiplayerRoute
{
	internal static Point Find(string biome)
	{
		if (biome is not ("forest" or "sahara" or "arctic" or "amazon"))
			throw new ArgumentException("unknown-route");
		for (int boundary = 400; boundary < Main.maxTilesX - 400; boundary += 200)
		{
			int x = boundary - 14;
			int y = Ground(x);
			if (y < 0 || !Matches(biome, x, y)) continue;
			bool walkable = true;
			for (int dx = 0; dx <= 42 && walkable; dx++)
			{
				int next = Ground(x + dx);
				walkable = next >= 0 && Math.Abs(next - y) <= 4;
				for (int dy = -4; dy < 0 && walkable; dy++)
				{
					Tile tile = Main.tile[x + dx, next + dy];
					walkable = tile.LiquidAmount == 0 &&
						(!tile.HasTile || !Main.tileSolid[tile.TileType]);
				}
			}
			if (walkable) return new Point(x, y);
		}
		throw new InvalidOperationException($"natural-walkable-section-crossing-not-found:{biome}");
	}
	private static int Ground(int x)
	{
		int bottom = Math.Min(Main.maxTilesY - 100, (int)Main.worldSurface + 200);
		for (int y = Math.Max(100, (int)Main.worldSurface - 700); y < bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]) return y;
		}
		return -1;
	}
	private static bool Matches(string biome, int x, int y)
	{
		int sand = 0, snow = 0, mud = 0;
		for (int depth = 0; depth < 24; depth++)
		{
			ushort type = Main.tile[x, y + depth].TileType;
			if (type is TileID.Sand or TileID.HardenedSand or TileID.Sandstone) sand++;
			if (type is TileID.SnowBlock or TileID.IceBlock) snow++;
			if (type is TileID.Mud or TileID.JungleGrass) mud++;
		}
		return biome switch
		{
			"sahara" => sand > 12, "arctic" => snow > 12, "amazon" => mud > 12,
			_ => sand + snow + mud == 0 && Main.tile[x, y].TileType == TileID.Grass
		};
	}
}
