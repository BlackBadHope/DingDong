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
	private static int DrainAndBuild(SpawnSanctuaryReceipt sanctuary, bool clearSpawnPocket)
	{
		int left = sanctuary.SafeArea.Left;
		int right = sanctuary.SafeArea.Right - 1;
		int top = sanctuary.SafeArea.Top;
		int bottom = sanctuary.SafeArea.Bottom - 1;
		int centerX = sanctuary.SpawnX;
		int floorY = sanctuary.FloorY;
		int cleared = 0;
		for (int x = left; x <= right; x++)
		{
			for (int y = top; y <= bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.LiquidAmount > 0)
				{
					tile.LiquidAmount = 0;
					cleared++;
				}
			}
		}

		// Build only a compact foundation under and immediately beside the house.
		// Existing authored solid cells are preserved so furniture/basements are not
		// flattened; empty cells are filled with readable mid-tone stone slabs.
		for (int x = left; x <= right; x++)
		{
			for (int y = floorY; y <= bottom; y++)
			{
				bool insideAuthoredAsset = sanctuary.AssetArea.Contains(x, y);
				PlaceFoundationTile(x, y, preserveSolid: insideAuthoredAsset);
			}
		}

		// Four-tile-high tapered sills stop surrounding FTW lava from flowing back
		// in while remaining ordinary jumpable landscaping instead of 33-tile towers.
		for (int offset = 0; offset < SillWidth; offset++)
		{
			int west = left + offset;
			int east = right - offset;
			int rise = Math.Max(2, SillHeight - offset);
			for (int y = floorY - rise; y <= bottom; y++)
			{
				PlaceSillTile(west, y);
				PlaceSillTile(east, y);
			}
		}

		if (clearSpawnPocket)
		{
			for (int x = centerX - SpawnPocketHalfWidth; x <= centerX + SpawnPocketHalfWidth; x++)
			{
				for (int y = floorY - SpawnPocketHeight; y < floorY; y++)
				{
					Tile tile = Main.tile[x, y];
					tile.ClearTile();
					tile.LiquidAmount = 0;
				}

				PlaceFoundationTile(x, floorY, preserveSolid: true);
			}
		}

		return cleared;
	}
}
