using System;
using System.Runtime;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldCapacity
{
	private const long GiB = 1024L * 1024L * 1024L;

	// Presets 28.10: admission uses the cells of the preset being loaded.
	internal static void AdmitClientLoad(ContinentalWorldPreset preset)
	{
		if (Main.dedServ) return;
		CompactTransientLoadGarbage();
		var full = ContinentalMemoryBudget.ForCapacity(
			preset.StorageWidth,
			preset.StorageHeight, true);
		bool tilesReady = Main.tile.Width == preset.StorageWidth &&
			Main.tile.Height == preset.StorageHeight;
		// RamOpt review fix 28.10: loading uses the fast rule only (low tiers are measured
		// for generation, not for loading or joining).
		Admit("load-menu", new ContinentalMemoryBudget(
			tilesReady ? 0 : full.TileBytes,
			HasClientMapCapacity(preset.StorageWidth, preset.StorageHeight) ? 0 : full.MapBytes,
			tilesReady ? ContinentalMemoryBudget.DeferredMapReserveBytes : full.ReserveBytes,
			tilesReady ? 0 : full.PhysicalFloorBytes), preset);
	}

	// Presets 28.10: the menu admits the selected preset (Main.maxTiles is still the carrier).
	internal static bool TryAdmitClientCreation(ContinentalWorldPreset preset, out string error)
	{
		try
		{
			CompactTransientLoadGarbage();
			PreflightMemory(preset.StorageWidth, preset.StorageHeight,
				allocateClientMap: false, generation: true, preset);
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

	private static void PreflightMemory(int storageWidth, int storageHeight,
		bool allocateClientMap, bool generation, ContinentalWorldPreset preset = null)
	{
		preset ??= ContinentalWorldDimensions.Current;
		if (!Environment.Is64BitProcess)
			throw new PlatformNotSupportedException("Continental worlds require 64-bit tModLoader.");
		// RamOpt review fix 28.10: a client that creates a world must also be able to
		// allocate the deferred map when it enters it, so creation counts the map now
		// (otherwise "generated, but cannot enter" after 30-60 minutes).
		bool includeMap = !Main.dedServ && (allocateClientMap || generation);
		ContinentalMemoryBudget budget = ContinentalMemoryBudget.ForCapacity(
			storageWidth, storageHeight, includeMap);
		// RamOpt 28.10: adaptive tiers instead of one hard threshold, but only for world
		// generation, where the economical code and the measurement apply. Loading a
		// world, a server load and a client joining an x10 server keep the fast rule.
		ContinentalMemoryBudget? lowMemory = generation
			? ContinentalMemoryBudget.ForCapacity(
				storageWidth, storageHeight, includeMap, lowMemory: true)
			: null;
		Admit("capacity", budget, preset, lowMemory, generation);
	}

	private static void PreflightDeferredClientMap(int storageWidth, int storageHeight)
	{
		ContinentalMemoryBudget budget = ContinentalMemoryBudget.ForDeferredMap(
			storageWidth, storageHeight);
		Admit("deferred-map", budget, ContinentalWorldDimensions.Current);
	}

	private static void Admit(string scope, ContinentalMemoryBudget budget, ContinentalWorldPreset preset,
		ContinentalMemoryBudget? lowMemory = null, bool generation = false)
	{
		ContinentalMemorySnapshot memory;
		// Presets 28.10 review fix: unreadable telemetry (non-Windows host, GlobalMemoryStatusEx
		// failure) is a memory denial, not a generic crash: the dedicated server then prints the
		// message and exits with ServerMemoryDeniedExitCode instead of a worldgen error that can
		// block on Console.ReadKey, and a failed load does not trigger the backup recovery.
		try { memory = ContinentalMemoryStatus.Read(); }
		catch (Exception ex) when (ex is PlatformNotSupportedException or System.ComponentModel.Win32Exception)
		{
			DingDongBiomeHomes.Instance?.Logger.Warn(
				$"DINGDONG_CONTINENTAL_MEMORY|verdict=DENIED|scope={scope}|generation={generation}|tier=telemetry-unavailable|reason={ex.Message}");
			throw new ContinentalMemoryAdmissionException(ex.Message);
		}
		// RamOpt 28.10: fast tier = previous rule; lower tiers only where measured.
		// Review fix: the pagefile-growth tier is opt-in (unverified on a real x10 run).
		// Owner, 30.10: loading (no low-memory tier) always counts page-file growth; see Evaluate.
		RamOptPagefileEstimate pagefile = !lowMemory.HasValue
			? (generation ? new(false, 0, "not-evaluated") : RamOptAdmissionPolicy.EstimatePagefileGrowth(memory))
			: RamOptAdmissionPolicy.PagefileTierEnabled
				? RamOptAdmissionPolicy.EstimatePagefileGrowth(memory)
				: new(false, 0, "disabled-default(" + RamOptAdmissionPolicy.PagefileOptInVariable + "=1 enables)");
		// Presets 28.10: denial text names the preset and the next smaller one that fits better.
		RamOptAdmission decision = RamOptAdmissionPolicy.Evaluate(budget, lowMemory, memory, pagefile,
			PresetLabel(preset), ContinentalWorldDimensions.NextSmaller(preset)?.Id);
		string planes = $"{RamOptTilePlanes.AllKnownBytesPerCell(out string known)}:{known}" +
			$"|registeredBytesPerCell={RamOptTilePlanes.RegisteredBytesPerCell(out string registered)}:{registered}";
		// Owner, 31.10: grow the page file now instead of mid-generation; refuse if Windows cannot.
		if (decision.Admitted && decision.Tier == RamOptAdmissionPolicy.PagefileTier)
		{
			bool grown = RamOptAdmissionPolicy.ForcePagefileGrowth(memory.AvailableCommitBytes, decision.PagefileCreditBytes);
			long limitAfter = -1, commitAfter = -1;
			try { var after = ContinentalMemoryStatus.Read(); limitAfter = after.CommitLimitBytes; commitAfter = after.AvailableCommitBytes; }
			catch (Exception ex) when (ex is PlatformNotSupportedException or System.ComponentModel.Win32Exception) { }
			DingDongBiomeHomes.Instance?.Logger.Info(
				$"DINGDONG_CONTINENTAL_MEMORY|pagefileGrowth={(grown ? "PASS" : "FAIL")}|scope={scope}" +
				$"|commitLimitBefore={memory.CommitLimitBytes}|commitLimitAfter={limitAfter}" +
				$"|commitBefore={memory.AvailableCommitBytes}|commitAfter={commitAfter}|credit={decision.PagefileCreditBytes}");
			if (!grown)
				decision = decision with { Admitted = false, Tier = RamOptAdmissionPolicy.DeniedTier,
					Message = DenyAfterGrowth(memory, decision) };
		}
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
			string warning = $"DingDong: {PresetLabel(preset)} world admitted in the slower low-memory mode (tier={decision.Tier}); " +
				$"normal mode needs {decision.FastRequired / (double)GiB:F2} GiB available commit, " +
				$"available {memory.AvailableCommitBytes / (double)GiB:F2} GiB. {(generation ? "Generation" : "Loading")} will take longer" +
				(decision.Tier == RamOptAdmissionPolicy.PagefileTier ? " and Windows will enlarge the page file." : ".");
			DingDongBiomeHomes.Instance?.Logger.Warn("DINGDONG_CONTINENTAL_MEMORY|warning=LOW_MEMORY_MODE|" + warning);
			if (Main.dedServ) Console.WriteLine(warning);
		}
	}

	private static string DenyAfterGrowth(ContinentalMemorySnapshot memory, RamOptAdmission decision)
	{
		bool ru = Terraria.Localization.Language.ActiveCulture?.Name?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
		double need = decision.PagefileCreditBytes / (double)GiB;
		return ru
			? $"Windows не смогла увеличить файл подкачки ещё на {need:F2} ГиБ (свободно commit {memory.AvailableCommitBytes / (double)GiB:F2} ГиБ). Закройте другие приложения и повторите."
			: $"Windows could not grow the page file by another {need:F2} GiB (available commit {memory.AvailableCommitBytes / (double)GiB:F2} GiB). Close other applications and retry.";
	}

	// Presets 28.10: "x10" for ram32 keeps the previous texts byte-identical.
	internal static string PresetLabel(ContinentalWorldPreset preset) =>
		preset == null || preset.IsX10 ? "x10" : preset.Id;

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
