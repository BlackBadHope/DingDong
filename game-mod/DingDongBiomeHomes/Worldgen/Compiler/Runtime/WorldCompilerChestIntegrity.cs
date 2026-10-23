using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerChestIntegrity
{
	internal static bool Present(int x, int y)
	{
		if (x < 1 || y < 1 || x + 1 >= Main.maxTilesX || y + 2 >= Main.maxTilesY) return false;
		Tile origin = Main.tile[x, y];
		if (!origin.HasTile || !TileID.Sets.BasicChest[origin.TileType] ||
			origin.TileFrameX % 36 != 0 || origin.TileFrameY % 36 != 0) return false;
		for (int dx = 0; dx < 2; dx++)
		{
			for (int dy = 0; dy < 2; dy++)
			{
				Tile tile = Main.tile[x + dx, y + dy];
				if (!tile.HasTile || tile.TileType != origin.TileType ||
					tile.TileFrameX != origin.TileFrameX + dx * 18 ||
					tile.TileFrameY != origin.TileFrameY + dy * 18) return false;
			}
			Tile floor = Main.tile[x + dx, y + 2];
			if (!floor.HasTile || (!Main.tileSolid[floor.TileType] && !Main.tileSolidTop[floor.TileType])) return false;
		}
		return true;
	}
}
