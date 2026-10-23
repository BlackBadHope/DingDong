using System;
using System.Runtime;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldCapacity
{
	private const long GiB = 1024L * 1024L * 1024L;

	internal static void AdmitClientLoad()
	{
		if (Main.dedServ) return;
		CompactTransientLoadGarbage();
		var full = ContinentalMemoryBudget.ForCapacity(
			ContinentalWorldDimensions.StorageWidth,
			ContinentalWorldDimensions.StorageHeight, true);
		bool tilesReady = Main.tile.Width == ContinentalWorldDimensions.StorageWidth &&
			Main.tile.Height == ContinentalWorldDimensions.StorageHeight;
		Admit("load-menu", new ContinentalMemoryBudget(
			tilesReady ? 0 : full.TileBytes,
			HasClientMapCapacity() ? 0 : full.MapBytes,
			tilesReady ? ContinentalMemoryBudget.DeferredMapReserveBytes : full.ReserveBytes));
	}

	internal static bool TryAdmitClientCreation(out string error)
	{
		try
		{
			CompactTransientLoadGarbage();
			PreflightMemory(allocateClientMap: false);
			error = null;
			return true;
		}
		catch (Exception ex) when (ex is ContinentalMemoryAdmissionException or
			PlatformNotSupportedException or System.ComponentModel.Win32Exception)
		{
			error = ex.Message;
			DingDongBiomeHomes.Instance?.Logger.Warn(
				$"DINGDONG_CONTINENTAL_MEMORY|verdict=DENIED|scope=create-menu|mutated=0|reason={error}");
			return false;
		}
	}

	private static void PreflightMemory(bool allocateClientMap)
	{
		if (!Environment.Is64BitProcess)
			throw new PlatformNotSupportedException("Continental worlds require 64-bit tModLoader.");
		ContinentalMemoryBudget budget = ContinentalMemoryBudget.ForCapacity(
			ContinentalWorldDimensions.StorageWidth,
			ContinentalWorldDimensions.StorageHeight,
			allocateClientMap && !Main.dedServ);
		Admit("capacity", budget);
	}

	private static void PreflightDeferredClientMap()
	{
		ContinentalMemoryBudget budget = ContinentalMemoryBudget.ForDeferredMap(
			ContinentalWorldDimensions.StorageWidth,
			ContinentalWorldDimensions.StorageHeight);
		Admit("deferred-map", budget);
	}

	private static void Admit(string scope, ContinentalMemoryBudget budget)
	{
		ContinentalMemorySnapshot memory = ContinentalMemoryStatus.Read();
		Require(memory.AvailablePhysicalBytes, budget.RequiredPhysicalSafety,
			"available physical memory");
		Require(memory.AvailableCommitBytes, budget.RequiredHeadroom,
			"available commit");
		DingDongBiomeHomes.Instance?.Logger.Info(
			$"DINGDONG_CONTINENTAL_MEMORY|verdict=PASS|scope={scope}" +
			$"|physicalRequired={budget.RequiredPhysicalSafety}" +
			$"|commitRequired={budget.RequiredHeadroom}|tile={budget.TileBytes}" +
			$"|map={budget.MapBytes}|reserve={budget.ReserveBytes}" +
			$"|physical={memory.AvailablePhysicalBytes}" +
			$"|commit={memory.AvailableCommitBytes}");
	}

	private static void CompactTransientLoadGarbage()
	{
		long before = GC.GetGCMemoryInfo().TotalCommittedBytes;
		GCSettings.LargeObjectHeapCompactionMode =
			GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced,
			blocking: true, compacting: true);
		GC.WaitForPendingFinalizers();
		// A forced collection can keep empty regions committed. At this loading
		// boundary return them to Windows before reading available commit.
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive,
			blocking: true, compacting: true);
		DingDongBiomeHomes.Instance?.Logger.Info(
			$"DINGDONG_CONTINENTAL_GC|before={before}|after={GC.GetGCMemoryInfo().TotalCommittedBytes}");
	}

	private static void Require(long actual, long minimum, string name)
	{
		if (actual <= 0 || actual < minimum)
			throw new ContinentalMemoryAdmissionException(
				$"Continental world requires {minimum / (double)GiB:F2} GiB {name}; " +
				$"available={Math.Max(0, actual) / (double)GiB:F2} GiB.");
	}
}
