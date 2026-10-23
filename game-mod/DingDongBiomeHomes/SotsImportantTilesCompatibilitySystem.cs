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
public sealed partial class SotsImportantTilesCompatibilitySystem : ModSystem
{
	private const string VulnerableSotsVersion = "0.25.1.9";
	private const int TilesPerTick = 90000;
	private const int WorldMargin = 25;
	private static SotsImportantTilesCompatibilitySystem instance;
	private HookThreadTileResetting hookDelegate;
	private FieldInfo finishedThreadingField;
	private readonly List<LandmarkDescriptor> landmarks = new();
	private readonly Dictionary<ushort, List<LandmarkDescriptor>> pendingByTile = new();
	private bool scanActive;
	private int scanX;
	private int scanY;
	private long scannedTiles;
	private int scanTicks;
	private int mainThreadId;
}
