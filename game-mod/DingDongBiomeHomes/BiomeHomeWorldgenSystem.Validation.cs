using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
public sealed partial class BiomeHomeWorldgenSystem
{
	private static void VerifyTileEntityAnchors(Dictionary<Point16, TileAnchorSnapshot> snapshots, Rectangle area, string structureName)
	{
		foreach ((Point16 position, TileAnchorSnapshot before)in snapshots)
		{
			Tile tile = Main.tile[position.X, position.Y];
			if (before.HasTile == tile.HasTile && before.TileType == tile.TileType && before.FrameX == tile.TileFrameX && before.FrameY == tile.TileFrameY)
				continue;
			throw new InvalidOperationException($"Authored structure changed a protected TileEntity anchor: structure={structureName}; origin={area.X},{area.Y}; entity={position.X},{position.Y}; tile={before.TileType}:{before.FrameX}:{before.FrameY}->{tile.TileType}:{tile.TileFrameX}:{tile.TileFrameY}.");
		}
	}
}
