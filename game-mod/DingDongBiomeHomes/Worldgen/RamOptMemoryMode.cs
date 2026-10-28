using System;
using System.ComponentModel;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

// RamOpt 28.10: our own worldgen buffers pick a fast (in-memory, unchanged) or
// economical (streamed to a temporary file) representation. The decision is made
// once at world creation and refined per stage; it never changes what is written
// into the world, only where our temporary copies live. Tile planes are tModLoader's.
// The decision uses the smaller of free physical RAM and free Windows commit
// (GlobalMemoryStatusEx), not GC.TotalAvailableMemoryBytes, which is total RAM.
// Tests may force a mode with DINGDONG_RAMOPT_MODE=economical|fast.
internal static class RamOptMemoryMode
{
	private const long MiB = 1024L * 1024L;
	private const long GiB = 1024L * MiB;
	// After one of our buffers, at least this much must remain for what still follows
	// in generation: the final world save. Computed: vanilla SaveWorld holds the .wld
	// about 4.5 times (MemoryStream growth, ToArray, backup and validation reads);
	// measured x10 .wld = 96.6 MB (S5GenData) -> ~0.45 GB, rounded up to 1 GiB.
	internal const long RemainingGenerationReserveBytes = 1L * GiB;
	// World creation (non-continental): below this free memory our passes start economical.
	internal const long CreationThresholdBytes = 2L * GiB;

	private static string continentalAdmissionTier;
	private static bool generationEconomical;
	private static string generationReason = "not-started";

	internal static bool GenerationEconomical => generationEconomical;

	internal static string Forced
	{
		get
		{
			string value = Environment.GetEnvironmentVariable("DINGDONG_RAMOPT_MODE")?.Trim().ToLowerInvariant();
			return value is "economical" or "fast" ? value : null;
		}
	}

	// Called by the continental admission right before the world-sized allocation.
	internal static void NoteContinentalAdmission(string tier) => continentalAdmissionTier = tier;

	internal static void BeginGeneration(Mod mod)
	{
		MemoryReading reading = Read();
		string forced = Forced;
		string tier = continentalAdmissionTier;
		continentalAdmissionTier = null;
		if (forced != null)
		{
			generationEconomical = forced == "economical";
			generationReason = "forced";
		}
		else if (ContinentalWorldDimensions.IsContinental && tier != null)
		{
			// Continental planes may be charged lazily while passes write them, so the
			// live reading here overstates what is left; the admission tier does not.
			generationEconomical = tier != RamOptAdmissionPolicy.FastTier;
			generationReason = "continental-admission-" + tier;
		}
		else
		{
			generationEconomical = reading.AvailableBytes > 0 && reading.AvailableBytes < CreationThresholdBytes;
			generationReason = generationEconomical ? "low-available-memory" : "enough-available-memory";
		}
		Receipt(mod, "world-creation", generationEconomical, reading, CreationThresholdBytes, generationReason, forced);
	}

	internal static void EndGeneration()
	{
		generationEconomical = false;
		generationReason = "not-started";
		continentalAdmissionTier = null;
	}

	// True when one of our passes should keep its temporary buffer out of RAM.
	internal static bool ForStage(Mod mod, string stage, long bufferBytes)
	{
		MemoryReading reading = Read();
		string forced = Forced;
		bool economical;
		string reason;
		long threshold = checked(Math.Max(0, bufferBytes) + RemainingGenerationReserveBytes);
		if (forced != null)
		{
			economical = forced == "economical";
			reason = "forced";
		}
		else if (generationEconomical)
		{
			economical = true;
			reason = "generation-" + generationReason;
		}
		else
		{
			economical = reading.AvailableBytes > 0 && reading.AvailableBytes < threshold;
			reason = economical ? "buffer-would-not-leave-reserve" : "enough-available-memory";
		}
		Receipt(mod, stage, economical, reading, threshold, reason, forced, bufferBytes);
		return economical;
	}

	private static void Receipt(Mod mod, string stage, bool economical, MemoryReading reading,
		long threshold, string reason, string forced, long bufferBytes = -1)
	{
		mod?.Logger.Info($"DINGDONG_RAM_MODE|stage={stage}|mode={(economical ? "economical" : "fast")}" +
			$"|availableBytes={reading.AvailableBytes}|availablePhysicalBytes={reading.PhysicalBytes}" +
			$"|availableCommitBytes={reading.CommitBytes}|thresholdBytes={threshold}|bufferBytes={bufferBytes}" +
			$"|reason={reason}|forced={forced ?? "none"}|source={reading.Source}|world={Main.maxTilesX}x{Main.maxTilesY}");
	}

	internal readonly record struct MemoryReading(long AvailableBytes, long PhysicalBytes, long CommitBytes, string Source);

	internal static MemoryReading Read()
	{
		try
		{
			if (OperatingSystem.IsWindows())
			{
				ContinentalMemorySnapshot status = ContinentalMemoryStatus.Read();
				return new(Math.Min(status.AvailablePhysicalBytes, status.AvailableCommitBytes),
					status.AvailablePhysicalBytes, status.AvailableCommitBytes, "GlobalMemoryStatusEx");
			}
		}
		catch (Exception ex) when (ex is ContinentalMemoryAdmissionException or Win32Exception or PlatformNotSupportedException)
		{
			// Fall through to the runtime's own estimate below.
		}
		// Non-Windows fallback: memory load observed at the last GC (stale, approximate).
		GCMemoryInfo info = GC.GetGCMemoryInfo();
		long approx = Math.Max(0, info.TotalAvailableMemoryBytes - info.MemoryLoadBytes);
		return new(approx, approx, -1, "gc-last-collection-estimate");
	}
}
