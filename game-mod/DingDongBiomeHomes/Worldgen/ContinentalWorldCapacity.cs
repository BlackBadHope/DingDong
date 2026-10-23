using System;
using System.Reflection;
using Terraria;
using Terraria.Map;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldCapacity
{
	private static ConstructorInfo tilemapConstructor;
	private static readonly object allocationGate = new();
	private static int tileAllocations, mapAllocations;
	private static volatile bool clientMapDeferred;
	internal static bool ClientMapDeferred => clientMapDeferred;

	internal static void Initialize()
	{
		lock (allocationGate) InitializeLocked();
	}

	private static void InitializeLocked()
	{
		ContinentalWorldDimensions.Validate();
		tilemapConstructor = typeof(Tilemap).GetConstructor(
			BindingFlags.NonPublic | BindingFlags.Instance,
			binder: null,
			types: new[] { typeof(ushort), typeof(ushort) },
			modifiers: null) ?? throw new MissingMethodException("Terraria.Tilemap", ".ctor(ushort, ushort)");
		InitializeLegacyWorldGenBounds();
	}

	internal static void Reset()
	{
		lock (allocationGate) ResetLocked();
	}

	private static void ResetLocked()
	{
		tilemapConstructor = null;
		ResetLegacyWorldGenBounds();
		clientMapDeferred = false;
		tileAllocations = mapAllocations = 0;
	}

	internal static void Ensure(string reason, bool deferClientMap = false)
	{
		// Tilemap construction mutates shared TileData arrays and pinned handles.
		// Keep the size check, admission and publication in one critical section.
		lock (allocationGate) EnsureLocked(reason, deferClientMap);
	}

	private static void EnsureLocked(string reason, bool deferClientMap)
	{
		if (!ContinentalWorldDimensions.IsContinental)
		{
			RestoreVanillaCapacity();
			return;
		}
		if (Main.tile.Width == ContinentalWorldDimensions.StorageWidth &&
			Main.tile.Height == ContinentalWorldDimensions.StorageHeight)
		{
			if (deferClientMap)
				DeferClientMap();
			else
				EnsureClientMap();
			RefreshRuntimeCompanions();
			LogCapacity(reason);
			return;
		}

		CompactTransientLoadGarbage();
		PreflightMemory(allocateClientMap: !deferClientMap);
		ResizeCore(ContinentalWorldDimensions.StorageWidth,
			ContinentalWorldDimensions.StorageHeight, !deferClientMap);
		LogCapacity(reason);
	}

	private static void RestoreVanillaCapacity()
	{
		// Startup/unload clearWorld calls can run before a world header exists.
		if (Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
			return;
		(int width, int height) = StorageForWorld(Main.maxTilesX, Main.maxTilesY);
		bool leavingContinental =
			Main.tile.Width == ContinentalWorldDimensions.StorageWidth &&
			Main.tile.Height == ContinentalWorldDimensions.StorageHeight;
		bool undersized = Main.tile.Width < width || Main.tile.Height < height;
		if (leavingContinental || undersized)
			ResizeCore(width, height, allocateClientMap: true);
	}

	internal static (int Width, int Height) StorageForWorld(int worldWidth, int worldHeight)
	{
		if (worldWidth <= 0 || worldHeight <= 0 ||
			worldWidth >= short.MaxValue || worldHeight >= short.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(worldWidth),
				$"World dimensions cannot use signed multiplayer coordinates: {worldWidth}x{worldHeight}.");
		return (checked(worldWidth + 1), checked(worldHeight + 1));
	}

	private static void ResizeCore(int width, int height, bool allocateClientMap)
	{
		Main.tile = (Tilemap)tilemapConstructor.Invoke(new object[] { (ushort)width, (ushort)height });
		tileAllocations++;
		if (!Main.dedServ)
		{
			Main.Map = allocateClientMap ? new WorldMap(width, height) : null;
			if (allocateClientMap) mapAllocations++;
			clientMapDeferred = !allocateClientMap;
		}
		RefreshRuntimeCompanions(resetSections: true);
	}

	internal static void EnsureClientMap()
	{
		lock (allocationGate) EnsureClientMapLocked();
	}

	private static void EnsureClientMapLocked()
	{
		if (Main.dedServ || !ContinentalWorldDimensions.IsContinental)
			return;
		if (HasClientMapCapacity())
		{
			clientMapDeferred = false;
			return;
		}
		PreflightDeferredClientMap();
		Main.Map = new WorldMap(ContinentalWorldDimensions.StorageWidth,
			ContinentalWorldDimensions.StorageHeight);
		mapAllocations++;
		clientMapDeferred = false;
		DingDongBiomeHomes.Instance.Logger.Info(
			"DINGDONG_CONTINENTAL_MAP|verdict=PASS|allocation=deferred" +
			$"|cells={ContinentalWorldDimensions.StorageCellCount}");
	}

	private static void DeferClientMap()
	{
		if (Main.dedServ) return;
		// WorldData can arrive again after clearWorld/playWorldCallBack has
		// allocated the map. Deferral must not discard that ready instance:
		// drawing may already be active and there may be no later callback
		// to allocate it again.
		if (HasClientMapCapacity())
		{
			clientMapDeferred = false;
			return;
		}
		Main.Map = null;
		clientMapDeferred = true;
	}

	private static bool HasClientMapCapacity() =>
		Main.Map != null &&
		Main.Map.MaxWidth >= ContinentalWorldDimensions.StorageWidth &&
		Main.Map.MaxHeight >= ContinentalWorldDimensions.StorageHeight;

}
