using System;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerProgressionChests
{
	private static void PlaceAt(string kind, int x, int y, int primary, int style,
		WorldCompilerProgressionReceipt receipt)
	{
		for (int tx = x; tx <= x + 1; tx++)
		{
			for (int ty = y - 1; ty <= y; ty++)
				if (Main.tile[tx, ty].HasTile)
					throw new InvalidOperationException($"Progression chest site obstructed: {kind} {tx},{ty}.");
			Tile floor = Main.tile[tx, y + 1];
			if (!floor.HasTile || !Main.tileSolid[floor.TileType])
				throw new InvalidOperationException($"Progression chest floor missing: {kind} {tx},{y + 1}.");
		}
		// Native loot filling; the verified floor bounds the downward search.
		if (!WorldGen.AddBuriedChest(x + 1, y, primary, false, style))
			throw new InvalidOperationException($"Progression chest placement failed: {kind} {x},{y}.");
		int index = Chest.FindChest(x, y - 1);
		if (index < 0)
			throw new InvalidOperationException($"Progression chest coordinates mismatch: {kind} {x},{y}.");
		bool locked = style is 2 or 4;
		if (Chest.IsLocked(x, y - 1) != locked)
			throw new InvalidOperationException($"Progression chest lock mismatch: {kind}.");
		Item first = Main.chest[index].item[0];
		if (WorldCompilerChestPrimaryPolicy.Restore(kind, style, first.type, primary, WorldGen.getGoodWorldGen))
		{
			first.SetDefaults(primary);
			first.Prefix(-1);
			receipt.NativePrimaryRestorations++;
			global::DingDongBiomeHomes.DingDongBiomeHomes.Instance.Logger.Info(
				$"DINGDONG_WORLD_COMPILER_PRIMARY|verdict=RESTORED|reason=ftw-jungle-AngelStatue|at={x},{y - 1}|primary={primary}");
		}
		if (first.type != primary)
			throw new InvalidOperationException($"Progression chest content mismatch: {kind} {x},{y}.");
		receipt.Chests.Add(new(kind, index, x, y - 1, primary, locked));
	}
}
