using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;
public static partial class AuthoredSettlementBuilder
{
	// Import is the only writer. Housing queries must not touch source margins.
	internal static void FrameAuthoredMutation(Rectangle area)
	{
		int left = Math.Max(10, area.Left - 2);
		int right = Math.Min(Main.maxTilesX - 10, area.Right + 2);
		int top = Math.Max(10, area.Top - 2);
		int bottom = Math.Min(Main.maxTilesY - 10, area.Bottom + 2);
		for (int x = left; x < right; x++)
		{
			for (int y = top; y < bottom; y++)
			{
				WorldGen.SquareTileFrame(x, y, true);
				try
				{
					WorldGen.SquareWallFrame(x, y, true);
				}
				catch (Exception exception)
				{
					Tile tile = Main.tile[x, y];
					throw new InvalidOperationException($"Wall framing failed at {x},{y}; tile={tile.TileType}; hasTile={tile.HasTile}; wall={tile.WallType}; world={Main.maxTilesX}x{Main.maxTilesY}.", exception);
				}
			}
		}
	}
}
