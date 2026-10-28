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
		// RamOpt review fix 28.10: loading uses the fast rule only (low tiers are measured
		// for generation, not for loading or joining).
		Admit("load-menu", new ContinentalMemoryBudget(
			tilesReady ? 0 : full.TileBytes,
			HasClientMapCapacity() ? 0 : full.MapBytes,
			tilesReady ? ContinentalMemoryBudget.DeferredMapReserveBytes : full.ReserveBytes,
			tilesReady ? 0 : full.PhysicalFloorBytes));
	}

	internal static bool TryAdmitClientCreation(out string error)
	{
		try
		{
			CompactTransientLoadGarbage();
			PreflightMemory(allocateClientMap: false, generation: true);
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

	private static void PreflightMemory(bool allocateClientMap, bool generation)
	{
		if (!Environment.Is64BitProcess)
			throw new PlatformNotSupportedException("Continental worlds require 64-bit tModLoader.");
		// RamOpt review fix 28.10: a client that creates a world must also be able to
		// allocate the deferred map when it enters it, so creation counts the map now
		// (otherwise "generated, but cannot enter" after 30-60 minutes).
		bool includeMap = !Main.dedServ && (allocateClientMap || generation);
		ContinentalMemoryBudget budget = ContinentalMemoryBudget.ForCapacity(
			ContinentalWorldDimensions.StorageWidth,
			ContinentalWorldDimensions.StorageHeight, includeMap);
		// RamOpt 28.10: adaptive tiers instead of one hard threshold, but only for world
		// generation, where the economical code and the measurement apply. Loading a
		// world, a server load and a client joining an x10 server keep the fast rule.
		ContinentalMemoryBudget? lowMemory = generation
			? ContinentalMemoryBudget.ForCapacity(
				ContinentalWorldDimensions.StorageWidth,
				ContinentalWorldDimensions.StorageHeight, includeMap, lowMemory: true)
			: null;
		Admit("capacity", budget, lowMemory, generation);
	}

	private static void PreflightDeferredClientMap()
	{
		ContinentalMemoryBudget budget = ContinentalMemoryBudget.ForDeferredMap(
			ContinentalWorldDimensions.StorageWidth,
			ContinentalWorldDimensions.StorageHeight);
		Admit("deferred-map", budget);
	}

	private static void Admit(string scope, ContinentalMemoryBudget budget,
		ContinentalMemoryBudget? lowMemory = null, bool generation = false)
	{
		ContinentalMemorySnapshot memory = ContinentalMemoryStatus.Read();
		// RamOpt 28.10: fast tier = previous rule; lower tiers only where measured.
		// Review fix: the pagefile-growth tier is opt-in (unverified on a real x10 run).
		RamOptPagefileEstimate pagefile = !lowMemory.HasValue
			? new(false, 0, "not-evaluated")
			: RamOptAdmissionPolicy.PagefileTierEnabled
				? RamOptAdmissionPolicy.EstimatePagefileGrowth(memory)
				: new(false, 0, "disabled-default(" + RamOptAdmissionPolicy.PagefileOptInVariable + "=1 enables)");
		RamOptAdmission decision = RamOptAdmissionPolicy.Evaluate(budget, lowMemory, memory, pagefile);
		string planes = $"{RamOptTilePlanes.AllKnownBytesPerCell(out string known)}:{known}" +
			$"|registeredBytesPerCell={RamOptTilePlanes.RegisteredBytesPerCell(out string registered)}:{registered}";
		if (!decision.Admitted)
		{
			DingDongBiomeHomes.Instance?.Logger.Warn(
				$"DINGDONG_CONTINENTAL_MEMORY|verdict=DENIED|scope={scope}|generation={generation}|tier={decision.Tier}" +
				$"|commitRequired={decision.FastRequired}|lowMemoryCommitRequired={decision.LowMemoryRequired}" +
				$"|physicalRequired={decision.PhysicalRequired}|physical={memory.AvailablePhysicalBytes}" +
				$"|commit={memory.AvailableCommitBytes}|commitLimit={memory.CommitLimitBytes}" +
				$"|pagefile={pagefile.Detail}|pagefileGrowth={pagefile.GrowthBytes}|planeBytesPerCell={planes}");
			throw new ContinentalMemoryAdmissionException(decision.Message);
		}
		if (generation)
			RamOptMemoryMode.NoteContinentalAdmission(decision.Tier);
		DingDongBiomeHomes.Instance?.Logger.Info(
			$"DINGDONG_CONTINENTAL_MEMORY|verdict=PASS|scope={scope}|generation={generation}" +
			$"|physicalRequired={decision.PhysicalRequired}" +
			$"|commitRequired={budget.RequiredHeadroom}|tile={budget.TileBytes}" +
			$"|map={budget.MapBytes}|reserve={budget.ReserveBytes}" +
			$"|physical={memory.AvailablePhysicalBytes}" +
			$"|commit={memory.AvailableCommitBytes}" +
			$"|tier={decision.Tier}|lowMemoryCommitRequired={decision.LowMemoryRequired}" +
			$"|pagefileCredit={decision.PagefileCreditBytes}|pagefileGrowth={pagefile.GrowthBytes}" +
			$"|commitLimit={memory.CommitLimitBytes}|planeBytesPerCell={planes}");
		if (decision.Tier != RamOptAdmissionPolicy.FastTier)
		{
			string warning = $"DingDong: x10 world admitted in the slower low-memory mode (tier={decision.Tier}); " +
				$"normal mode needs {decision.FastRequired / (double)GiB:F2} GiB available commit, " +
				$"available {memory.AvailableCommitBytes / (double)GiB:F2} GiB. Generation will take longer" +
				(decision.Tier == RamOptAdmissionPolicy.PagefileTier ? " and Windows will enlarge the page file." : ".");
			DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_CONTINENTAL_MEMORY|warning=LOW_MEMORY_MODE|" + warning);
			if (Main.dedServ) Console.WriteLine(warning);
		}
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

	// RamOpt 28.10 (ported from the unfinished edit, restricted to continental worlds by
	// its caller): after generation our pass buffers and vanilla/provider worldgen
	// temporaries are garbage; return the freed regions to Windows before the final save
	// instead of keeping the generation peak committed.
	internal static void ReleaseTransientGarbage(string stage)
	{
		long before = GC.GetGCMemoryInfo().TotalCommittedBytes;
		long privateBefore = ProcessPrivateBytes();
		GCSettings.LargeObjectHeapCompactionMode =
			GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced,
			blocking: true, compacting: true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive,
			blocking: true, compacting: true);
		DingDongBiomeHomes.Instance?.Logger.Info(
			$"DINGDONG_RAMOPT_GC|stage={stage}|before={before}|after={GC.GetGCMemoryInfo().TotalCommittedBytes}" +
			$"|privateBefore={privateBefore}|privateAfter={ProcessPrivateBytes()}");
	}

	internal static long ProcessPrivateBytes()
	{
		using var process = System.Diagnostics.Process.GetCurrentProcess();
		return process.PrivateMemorySize64;
	}
}
