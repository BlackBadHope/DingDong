using System;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsGemPlacement
{
	internal static void PlaceSapphire()
	{
		int[] minimum = GenVars.snowMinX;
		int[] maximum = GenVars.snowMaxX;
		int top = Math.Max(100, GenVars.snowTop);
		int bottom = Math.Min(Main.maxTilesY - 100, GenVars.snowBottom);
		if (minimum is null || maximum is null || top > bottom ||
			minimum.Length <= bottom || maximum.Length <= bottom)
			throw new InvalidOperationException("SOTS Sapphire snow bounds are unavailable.");

		int center = Main.maxTilesX / 2;
		int snowCenter = GenVars.snowOriginLeft +
			(GenVars.snowOriginRight - GenVars.snowOriginLeft) / 2;
		int direction = snowCenter < center ? -1 : 1;
		for (int y = bottom; y >= top; y--)
		{
			int edge = direction < 0 ? minimum[y] : maximum[y];
			for (int inward = 0; inward <= 32; inward++)
			{
				int x = edge - direction * inward;
				if (!WorldGen.InWorld(x, y, 100) || !IsSnow(x, y)) continue;
				int placeX = x + 20 * direction;
				if (!WorldGen.InWorld(placeX, y, 50))
					throw new InvalidOperationException("SOTS Sapphire resolved outside safe world bounds.");
				GenerateSapphire(placeX, y);
				SotsWorldgenReceipt.MarkSapphire();
				return;
			}
		}
		throw new InvalidOperationException("SOTS Sapphire found no generated snow-edge anchor.");
	}

	private static bool IsSnow(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile && (tile.TileType == TileID.SnowBlock || tile.TileType == TileID.IceBlock);
	}
}
