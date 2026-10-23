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
	private static bool TryPlanCollision(Rectangle protectedArea, PersistentObjectCollision collision, IReadOnlyList<Rectangle> forbiddenAreas, out ChestRelocationPlan plan, out string failure)
	{
		plan = null;
		if (!IsSupportedSourceTile(collision, out failure))
			return false;
		int sourceIndex = Chest.FindChest(collision.Position.X, collision.Position.Y);
		if (sourceIndex < 0 || Main.chest[sourceIndex] == null)
		{
			failure = $"source-chest-not-found:{collision.Position.X},{collision.Position.Y}";
			return false;
		}

		Tile sourceTile = Main.tile[collision.Position.X, collision.Position.Y];
		ushort tileType = sourceTile.TileType;
		int style = Math.Max(0, sourceTile.TileFrameX / 36);
		Chest source = Main.chest[sourceIndex];
		string signature = ComputeSignature(source);
		int itemCount = CountItems(source);
		if (!TryFindDestination(protectedArea, collision.Position, forbiddenAreas, out Point16 destination))
		{
			failure = "lossless-chest-destination-not-found";
			return false;
		}

		plan = new ChestRelocationPlan(sourceIndex, collision.Position, destination, tileType, style, itemCount, signature);
		failure = string.Empty;
		return true;
	}
}
