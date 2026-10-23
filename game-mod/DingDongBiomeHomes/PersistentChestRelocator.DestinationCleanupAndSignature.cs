using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace DingDongBiomeHomes;
/// <summary>
/// Lossless chest relocation for dedicated probes and authored worldgen. The
/// existing chest index is rebound to a verified destination before source
/// tiles are cleared, so full modded chest arrays remain safe.
/// </summary>
public static partial class PersistentChestRelocator
{
	private static void CopyChestTileAppearance(Point16 sourceTopLeft, Point16 destinationTopLeft)
	{
		for (int x = 0; x < 2; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				Tile source = Main.tile[sourceTopLeft.X + x, sourceTopLeft.Y + y];
				Tile destination = Main.tile[destinationTopLeft.X + x, destinationTopLeft.Y + y];
				destination.TileColor = source.TileColor;
				destination.IsTileInvisible = source.IsTileInvisible;
				destination.IsTileFullbright = source.IsTileFullbright;
			}
		}
	}

	private static void RemovePartialDestination(Point16 topLeft)
	{
		int destinationIndex = Chest.FindChest(topLeft.X, topLeft.Y);
		if (destinationIndex >= 0 && Main.chest[destinationIndex] != null)
			RemoveDestination(topLeft, destinationIndex);
		else
			ClearChestTiles(topLeft);
	}

	private static void RemoveDestination(Point16 topLeft, int destinationIndex)
	{
		Chest destination = Main.chest[destinationIndex];
		if (destination != null)
		{
			foreach (Item item in destination.item)
				item.TurnToAir();
			destination.name = string.Empty;
			Chest.DestroyChest(topLeft.X, topLeft.Y);
		}

		ClearChestTiles(topLeft);
	}

	private static int CountItems(Chest chest)
	{
		int count = 0;
		foreach (Item item in chest.item)
		{
			if (!item.IsAir)
				count++;
		}

		return count;
	}

	private static string ComputeSignature(Chest chest)
	{
		ulong hash = 1469598103934665603UL;
		Mix(ref hash, chest.name ?? string.Empty);
		foreach (Item item in chest.item)
		{
			Mix(ref hash, item.type);
			Mix(ref hash, item.stack);
			Mix(ref hash, item.prefix);
		}

		return hash.ToString("X16");
	}

	private static void Mix(ref ulong hash, int value)
	{
		unchecked
		{
			hash ^= (uint)value;
			hash *= 1099511628211UL;
		}
	}

	private static void Mix(ref ulong hash, string value)
	{
		foreach (char character in value)
			Mix(ref hash, character);
	}
}
