using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// SOTS 0.25.1.9 queues a full Main.tile scan on ThreadPool. A client can join
/// while the server is still changing world/tile state, which makes that worker
/// observe an invalid Tilemap index and terminate the server. Keep SOTS' own
/// landmark objects and packet protocol, but perform the scan incrementally on
/// the main game thread.
/// </summary>
public sealed partial class SotsImportantTilesCompatibilitySystem
{
	public override void PostUpdateEverything()
	{
		// Dedicated servers can legitimately retain Main.gameMenu=true while the
		// world simulation is active. Filtering that flag starts the replacement
		// scan but never advances it after the first network handshake.
		if (!scanActive || Main.netMode == NetmodeID.MultiplayerClient)
			return;
		if (Environment.CurrentManagedThreadId != mainThreadId)
			throw new InvalidOperationException("DingDong SOTS tile scan left the main game thread.");
		int processed = 0;
		while (scanActive && processed < TilesPerTick)
		{
			if (scanX >= Main.maxTilesX - WorldMargin)
			{
				CompleteScan();
				break;
			}

			Tile tile = Main.tile[scanX, scanY];
			if (tile.HasTile && pendingByTile.TryGetValue(tile.TileType, out List<LandmarkDescriptor> matching))
			{
				for (int index = matching.Count - 1; index >= 0; index--)
				{
					LandmarkDescriptor descriptor = matching[index];
					descriptor.AssignPoint.Invoke(descriptor.Instance, new object[] { tile, scanX, scanY, false });
					if (descriptor.Position.GetValue(descriptor.Instance) != null)
						matching.RemoveAt(index);
				}

				if (matching.Count == 0)
					pendingByTile.Remove(tile.TileType);
			}

			scannedTiles++;
			processed++;
			scanY++;
			if (scanY >= Main.maxTilesY - WorldMargin)
			{
				scanY = WorldMargin;
				scanX++;
			}
		}

		scanTicks++;
	}
}
