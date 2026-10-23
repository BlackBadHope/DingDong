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
	private void BeginScan()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
			return;
		mainThreadId = Environment.CurrentManagedThreadId;
		scanX = WorldMargin;
		scanY = WorldMargin;
		scannedTiles = 0;
		scanTicks = 0;
		pendingByTile.Clear();
		foreach (LandmarkDescriptor descriptor in landmarks)
		{
			descriptor.Position.SetValue(descriptor.Instance, null);
			if (!pendingByTile.TryGetValue(descriptor.TileType, out List<LandmarkDescriptor> matching))
			{
				matching = new List<LandmarkDescriptor>();
				pendingByTile.Add(descriptor.TileType, matching);
			}

			matching.Add(descriptor);
		}

		finishedThreadingField.SetValue(null, false);
		scanActive = true;
		Mod.Logger.Info($"DINGDONG_SOTS_TILE_SCAN|verdict=STARTED|mode=main-thread-incremental|thread={mainThreadId}|world={Main.maxTilesX}x{Main.maxTilesY}|landmarks={landmarks.Count}");
	}

	private void CompleteScan()
	{
		scanActive = false;
		finishedThreadingField.SetValue(null, true);
		int found = 0;
		List<string> missing = new();
		foreach (LandmarkDescriptor descriptor in landmarks)
		{
			if (descriptor.Position.GetValue(descriptor.Instance) != null)
				found++;
			else missing.Add(ModContent.GetModTile(descriptor.TileType)?.FullName ?? $"tile:{descriptor.TileType}");
		}

		Mod.Logger.Info($"DINGDONG_SOTS_TILE_SCAN|verdict=PASS|scope=scan-completion|mode=main-thread-incremental|thread={mainThreadId}|tiles={scannedTiles}|ticks={scanTicks + 1}|landmarksFound={found}/{landmarks.Count}|workerThreads=0");
		if (missing.Count > 0)
			Mod.Logger.Warn($"DINGDONG_SOTS_LANDMARK_COVERAGE|found={found}/{landmarks.Count}|missing=[{string.Join(';', missing)}]|generationRequirement=provider-dependent");
	}
}
