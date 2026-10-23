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
	public static bool TryVerify(Point16 sourceTopLeft, Point16 destinationTopLeft, string expectedSignature, out string failure)
	{
		if (Chest.FindChest(sourceTopLeft.X, sourceTopLeft.Y) >= 0)
		{
			failure = "source-chest-still-registered";
			return false;
		}

		int destinationIndex = Chest.FindChest(destinationTopLeft.X, destinationTopLeft.Y);
		if (destinationIndex < 0 || Main.chest[destinationIndex] == null)
		{
			failure = "destination-chest-not-registered";
			return false;
		}

		string actualSignature = ComputeSignature(Main.chest[destinationIndex]);
		if (!string.Equals(actualSignature, expectedSignature, StringComparison.Ordinal))
		{
			failure = $"destination-signature-mismatch:{actualSignature}";
			return false;
		}

		failure = string.Empty;
		return true;
	}
}
