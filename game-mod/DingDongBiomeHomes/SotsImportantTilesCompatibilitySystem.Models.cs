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
	private delegate void OrigThreadTileResetting();
	private delegate void HookThreadTileResetting(OrigThreadTileResetting orig);
	private sealed record LandmarkDescriptor(object Instance, ushort TileType, FieldInfo Position, MethodInfo AssignPoint);
}
