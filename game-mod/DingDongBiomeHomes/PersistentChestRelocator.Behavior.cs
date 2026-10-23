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
	/// <summary>
	/// Fast, non-mutating admission used while scanning candidate sites. Some
	/// content mods leave a Main.chest record at a tile later replaced by another
	/// worldgen pass. Such a record is persistent data, but it is not a supported
	/// 2x2 chest and must make that candidate ineligible instead of failing the
	/// completed 61-site batch plan.
	/// </summary>
	public static bool IsSupportedSourceTile(PersistentObjectCollision collision, out string failure)
	{
		if (!string.Equals(collision.Kind, "Chest", StringComparison.Ordinal))
		{
			failure = $"unsupported-collision:{collision}";
			return false;
		}

		if (!WorldGen.InWorld(collision.Position.X, collision.Position.Y, 1))
		{
			failure = $"source-out-of-world:{collision.Position.X},{collision.Position.Y}";
			return false;
		}

		Tile sourceTile = Main.tile[collision.Position.X, collision.Position.Y];
		if (!sourceTile.HasTile || !TileID.Sets.BasicChest[sourceTile.TileType])
		{
			failure = $"source-tile-not-chest:{sourceTile.TileType}";
			return false;
		}

		failure = string.Empty;
		return true;
	}
}
