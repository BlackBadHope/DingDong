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
	public static bool TryApply(ChestRelocationPlan plan, out ChestRelocationReceipt receipt, out string failure)
	{
		receipt = null;
		int sourceIndex = Chest.FindChest(plan.SourceTopLeft.X, plan.SourceTopLeft.Y);
		if (sourceIndex != plan.SourceIndex || Main.chest[sourceIndex] == null)
		{
			failure = "source-chest-changed-before-apply";
			return false;
		}

		Chest source = Main.chest[sourceIndex];
		if (!string.Equals(ComputeSignature(source), plan.ItemSignature, StringComparison.Ordinal))
		{
			failure = "source-item-signature-changed-before-apply";
			return false;
		}

		Item[] snapshots = new Item[source.item.Length];
		for (int index = 0; index < source.item.Length; index++)
			snapshots[index] = source.item[index].Clone();
		string sourceName = source.name;
		// PlaceChest applies vanilla/mod TileObjectData admission and can reject a
		// perfectly valid relocation for modded BasicChest tiles. PlaceChestDirect
		// is the exact tML network-sync primitive for a validated 2x2 chest and lets
		// us reuse the existing record index without ever needing a free slot.
		ClearChestTiles(plan.DestinationTopLeft);
		Main.chest[sourceIndex] = null;
		try
		{
			WorldGen.PlaceChestDirect(plan.DestinationTopLeft.X, plan.DestinationTopLeft.Y + 1, plan.TileType, plan.Style, sourceIndex);
		}
		catch (Exception exception)
		{
			RemovePartialDestination(plan.DestinationTopLeft);
			Main.chest[sourceIndex] = source;
			failure = $"destination-direct-placement-threw:{exception.GetType().Name}";
			return false;
		}

		int destinationIndex = Chest.FindChest(plan.DestinationTopLeft.X, plan.DestinationTopLeft.Y);
		if (destinationIndex != sourceIndex || Main.chest[destinationIndex] == null || !DestinationTilesMatch(plan))
		{
			RemovePartialDestination(plan.DestinationTopLeft);
			Main.chest[sourceIndex] = source;
			failure = "destination-direct-placement-verification-failed";
			return false;
		}

		CopyChestTileAppearance(plan.SourceTopLeft, plan.DestinationTopLeft);
		Chest destination = Main.chest[destinationIndex];
		for (int index = 0; index < source.item.Length; index++)
			destination.item[index] = snapshots[index].Clone();
		destination.name = sourceName;
		if (!string.Equals(ComputeSignature(destination), plan.ItemSignature, StringComparison.Ordinal))
		{
			RemoveDestination(plan.DestinationTopLeft, destinationIndex);
			Main.chest[sourceIndex] = source;
			failure = "destination-item-signature-mismatch";
			return false;
		}

		if (!TryVerify(plan.SourceTopLeft, plan.DestinationTopLeft, plan.ItemSignature, out string verifyFailure))
		{
			RemoveDestination(plan.DestinationTopLeft, destinationIndex);
			Main.chest[sourceIndex] = source;
			failure = $"post-apply-verify-failed:{verifyFailure}";
			return false;
		}

		ClearChestTiles(plan.SourceTopLeft);
		receipt = new ChestRelocationReceipt(plan.SourceTopLeft, plan.DestinationTopLeft, destinationIndex, plan.ItemCount, plan.ItemSignature, true);
		failure = string.Empty;
		return true;
	}
}
