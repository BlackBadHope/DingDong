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

	// RamOpt review fix 28.10: generation=true only for new-world generation; only there
	// may the admission use the measured low-memory tier.
	internal static void Ensure(string reason, bool deferClientMap = false, bool generation = false)
	{
		// Tilemap construction mutates shared TileData arrays and pinned handles.
		// Keep the size check, admission and publication in one critical section.
		lock (allocationGate) EnsureLocked(reason, deferClientMap, generation);
	}

	private static void EnsureLocked(string reason, bool deferClientMap, bool generation)
	{
		if (!ContinentalWorldDimensions.IsContinental)
		{
			RestoreVanillaCapacity();
			return;
		}
		// Presets 28.10: target storage of the current preset; "already sized" means an exact match.
		(int targetWidth, int targetHeight) = StorageForWorld(Main.maxTilesX, Main.maxTilesY);
		if (Main.tile.Width == targetWidth &&
			Main.tile.Height == targetHeight)
		{
			if (deferClientMap)
				DeferClientMap();
			else
				EnsureClientMap();
			RefreshRuntimeCompanions();
			LogCapacity(reason);
			return;
		}

		// RamOpt 28.10: shrink before growing. TileData<T>.SetLength allocates each new
		// plane while the previous (Large, ~0.32 GiB) planes are still alive, and the
		// admission below read commit with them charged. Release them first, compact,
		// then admit against what is really free. Only outside gameplay (menu, server,
		// world creation/load), where nothing else reads Main.tile; the old contents
		// are discarded by clearWorld/load anyway.
		int previousWidth = Main.tile.Width, previousHeight = Main.tile.Height;
		long privateBefore = ContinentalWorldCapacity.ProcessPrivateBytes();
		bool shrunk = (Main.dedServ || Main.gameMenu) && previousWidth > 1 && previousHeight > 1;
		if (shrunk)
			Main.tile = (Tilemap)tilemapConstructor.Invoke(new object[] { (ushort)1, (ushort)1 });
		CompactTransientLoadGarbage();
		long privateShrunk = ContinentalWorldCapacity.ProcessPrivateBytes();
		try
		{
			PreflightMemory(targetWidth, targetHeight, allocateClientMap: !deferClientMap, generation);
		}
		catch
		{
			// Denied (or telemetry failed): give back the previous capacity unchanged in size.
			if (shrunk)
				Main.tile = (Tilemap)tilemapConstructor.Invoke(new object[] { (ushort)previousWidth, (ushort)previousHeight });
			throw;
		}
		ResizeCore(targetWidth, targetHeight, !deferClientMap);
		// RamOpt 28.10: direct probe of the "lazy commit" anomaly seen in S5.
		DingDongBiomeHomes.Instance?.Logger.Info(
			$"DINGDONG_RAMOPT_RESIZE|reason={reason}|previous={previousWidth}x{previousHeight}|shrunkFirst={shrunk}" +
			$"|privateBefore={privateBefore}|privateAfterShrinkGc={privateShrunk}" +
			$"|privateAfterAllocation={ContinentalWorldCapacity.ProcessPrivateBytes()}" +
			$"|gcCommitted={GC.GetGCMemoryInfo().TotalCommittedBytes}" +
			$"|availableCommit={ReadAvailableCommitForLog()}");
		LogCapacity(reason);
	}

	private static long ReadAvailableCommitForLog()
	{
		try { return ContinentalMemoryStatus.Read().AvailableCommitBytes; }
		catch (Exception) { return -1; }
	}

	private static void RestoreVanillaCapacity()
	{
		// Startup/unload clearWorld calls can run before a world header exists.
		if (Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
			return;
		(int width, int height) = StorageForWorld(Main.maxTilesX, Main.maxTilesY);
		// Presets 28.10: leaving any continental preset (all are larger than vanilla Large
		// storage in both dimensions), not only x10.
		bool leavingContinental =
			Main.tile.Width > ContinentalWorldDimensions.VanillaLargeWidth + 1 &&
			Main.tile.Height > ContinentalWorldDimensions.VanillaLargeHeight + 1;
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
		// Presets 28.10: the map is sized exactly for the current preset.
		(int width, int height) = StorageForWorld(Main.maxTilesX, Main.maxTilesY);
		PreflightDeferredClientMap(width, height);
		Main.Map = new WorldMap(width, height);
		mapAllocations++;
		clientMapDeferred = false;
		DingDongBiomeHomes.Instance.Logger.Info(
			"DINGDONG_CONTINENTAL_MAP|verdict=PASS|allocation=deferred" +
			$"|cells={(long)width * height}");
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

	// Presets 28.10: exact match with the current preset (not >=), so a smaller preset after
	// x10 does not keep the x10 map committed.
	private static bool HasClientMapCapacity()
	{
		if (Main.maxTilesX <= 0 || Main.maxTilesY <= 0 ||
			Main.maxTilesX >= short.MaxValue || Main.maxTilesY >= short.MaxValue)
			return false;
		(int width, int height) = StorageForWorld(Main.maxTilesX, Main.maxTilesY);
		return HasClientMapCapacity(width, height);
	}

	private static bool HasClientMapCapacity(int width, int height) =>
		Main.Map != null &&
		Main.Map.MaxWidth == width &&
		Main.Map.MaxHeight == height;

}
