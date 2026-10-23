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
	public override void OnWorldUnload()
	{
		scanActive = false;
		pendingByTile.Clear();
	}

	public override void Unload()
	{
		instance = null;
		hookDelegate = null;
		finishedThreadingField = null;
		landmarks.Clear();
		pendingByTile.Clear();
		scanActive = false;
	}

	private static void DetourThreadTileResetting(OrigThreadTileResetting orig)
	{
		if (instance == null)
		{
			orig();
			return;
		}

		instance.BeginScan();
	}
}
