using System;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerProgressionHash
{
	internal static string Tiles(WorldCompilerRect bounds)
	{
		if (bounds.X < 1 || bounds.Y < 1 || bounds.Right >= Main.maxTilesX || bounds.Bottom >= Main.maxTilesY ||
			bounds.Width > 16 || bounds.Height > 16)
			throw new InvalidOperationException("Progression proof object escaped tile bounds.");
		ulong hash = 1469598103934665603UL;
		for (int x = bounds.X; x < bounds.Right; x++)
		for (int y = bounds.Y; y < bounds.Bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			Add(ref hash, tile.HasTile ? 1 : 0); Add(ref hash, tile.TileType);
			Add(ref hash, tile.WallType); Add(ref hash, tile.TileFrameX); Add(ref hash, tile.TileFrameY);
			// Liquid settling is legitimate between generation and the first save.
			// The proof covers object identity/frames, not surrounding fluid simulation.
			Add(ref hash, (int)tile.Slope); Add(ref hash, tile.IsHalfBlock ? 1 : 0);
		}
		return hash.ToString("X16");
	}

	internal static string Chest(int x, int y)
	{
		int index = Terraria.Chest.FindChest(x, y);
		if (index < 0) throw new InvalidOperationException($"Progression chest missing at {x},{y}.");
		ulong hash = 1469598103934665603UL;
		foreach (Item item in Main.chest[index].item)
		{
			Add(ref hash, item.type); Add(ref hash, item.stack); Add(ref hash, item.prefix);
		}
		return hash.ToString("X16");
	}

	private static void Add(ref ulong hash, int value)
	{
		hash ^= unchecked((uint)value);
		hash *= 1099511628211UL;
	}
}
