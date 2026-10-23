using Terraria;

namespace DingDongBiomeHomes.Acceptance;

internal static class X10MultiplayerTerrain
{
	internal static string Hash(int x, int y)
	{
		ulong hash = 14695981039346656037UL;
		for (int dx = -4; dx <= 4; dx++)
			for (int dy = 8; dy <= 16; dy++)
			{
				Tile tile = Main.tile[x + dx, y + dy];
				hash = unchecked((hash ^ (tile.HasTile ? 1UL : 0UL)) * 1099511628211UL);
				hash = unchecked((hash ^ tile.TileType) * 1099511628211UL);
				hash = unchecked((hash ^ tile.WallType) * 1099511628211UL);
			}
		return hash.ToString("X16");
	}
}
