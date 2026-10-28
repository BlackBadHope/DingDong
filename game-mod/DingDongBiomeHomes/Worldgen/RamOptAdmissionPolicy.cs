using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using Microsoft.Win32;
using Terraria.Localization;

namespace DingDongBiomeHomes.Worldgen;

// RamOpt 28.10: adaptive continental admission. Instead of one hard threshold:
//  fast            - today's requirement (planes + map + 2 GiB reserve), unchanged behaviour;
//  economical      - the measured minimum (planes + map + 1.5 GiB), our passes stream to disk;
//  pagefile-growth - economical, but up to 2 GiB of the shortfall is left to a Windows page
//                    file that can still grow (system-managed or custom maximum, disk room).
//                    Review fix 28.10: OFF by default. Planes are allocated lazily mid-pass
//                    (TileWallWireStateData alone is one 1.6 GB array), so a page file that
//                    does not grow in time ends in OutOfMemoryException after tens of minutes.
//                    Opt-in only (DINGDONG_RAMOPT_PAGEFILE_GROWTH=1) until one real x10 run
//                    in this tier has passed;
//  denied          - fail before any allocation with a message naming a preset that fits.
internal readonly record struct RamOptPagefileEstimate(bool Known, long GrowthBytes, string Detail);

internal readonly record struct RamOptAdmission(bool Admitted, string Tier, long FastRequired,
	long LowMemoryRequired, long PhysicalRequired, long PagefileCreditBytes, string Message);

internal static class RamOptAdmissionPolicy
{
	internal const string FastTier = "fast";
	internal const string EconomicalTier = "economical";
	internal const string PagefileTier = "pagefile-growth";
	internal const string DeniedTier = "denied";
	private const long MiB = 1024L * 1024L;
	private const long GiB = 1024L * MiB;
	// Never rely on more page-file growth than this for one world.
	internal const long MaxPagefileCreditBytes = 2L * GiB;
	internal const string PagefileOptInVariable = "DINGDONG_RAMOPT_PAGEFILE_GROWTH";

	// Review fix 28.10: the pagefile-growth tier is unverified, so it is opt-in.
	internal static bool PagefileTierEnabled
	{
		get
		{
			string value = Environment.GetEnvironmentVariable(PagefileOptInVariable)?.Trim();
			return value is "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	// Presets 28.10: presetLabel/smallerPresetId only change the denial text ("x10"/null = ram32
	// without a smaller released preset).
	internal static RamOptAdmission Evaluate(ContinentalMemoryBudget fast, ContinentalMemoryBudget? lowMemory,
		ContinentalMemorySnapshot memory, RamOptPagefileEstimate pagefile,
		string presetLabel = "x10", string smallerPresetId = null)
	{
		long physical = memory.AvailablePhysicalBytes, commit = memory.AvailableCommitBytes;
		long fastRequired = fast.RequiredHeadroom;
		if (physical > 0 && commit > 0 && physical >= fast.RequiredPhysicalSafety && commit >= fastRequired)
			return new(true, FastTier, fastRequired, lowMemory?.RequiredHeadroom ?? fastRequired,
				fast.RequiredPhysicalSafety, 0, null);
		if (lowMemory is ContinentalMemoryBudget low && physical > 0 && commit > 0 &&
			physical >= low.RequiredPhysicalSafety)
		{
			if (commit >= low.RequiredHeadroom)
				return new(true, EconomicalTier, fastRequired, low.RequiredHeadroom, low.RequiredPhysicalSafety, 0, null);
			long deficit = low.RequiredHeadroom - commit;
			if (pagefile.Known && deficit <= MaxPagefileCreditBytes && pagefile.GrowthBytes >= checked(2 * deficit))
				return new(true, PagefileTier, fastRequired, low.RequiredHeadroom, low.RequiredPhysicalSafety, deficit, null);
		}
		ContinentalMemoryBudget floor = lowMemory ?? fast;
		bool physicalShort = physical <= 0 || physical < floor.RequiredPhysicalSafety;
		return new(false, DeniedTier, fastRequired, floor.RequiredHeadroom, floor.RequiredPhysicalSafety, 0,
			DenialMessage(fast, floor, lowMemory.HasValue, physical, commit, pagefile, physicalShort,
				presetLabel ?? "x10", smallerPresetId));
	}

	private static string DenialMessage(ContinentalMemoryBudget fast, ContinentalMemoryBudget floor,
		bool lowTier, long physical, long commit, RamOptPagefileEstimate pagefile, bool physicalShort,
		string presetLabel, string smallerPresetId)
	{
		static string G(long bytes) => (Math.Max(0, bytes) / (double)GiB).ToString("F2");
		bool ru = Language.ActiveCulture?.Name?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true;
		if (ru)
		{
			string first = physicalShort
				? $"Континентальному миру нужно {G(floor.RequiredPhysicalSafety)} ГиБ свободной ОЗУ; доступно {G(physical)} ГиБ."
				: $"Континентальному миру нужно {G(floor.RequiredHeadroom)} ГиБ доступного лимита выделения (commit); доступно {G(commit)} ГиБ.";
			string pf = pagefile.Known ? $"; файл подкачки может вырасти ещё примерно на {G(pagefile.GrowthBytes)} ГиБ" : "";
			return first +
				$" Обычный режим {(presetLabel == "x10" ? "×10" : presetLabel)}: {G(fast.RequiredHeadroom)} ГиБ commit и {G(fast.RequiredPhysicalSafety)} ГиБ свободной ОЗУ" +
				(lowTier ? $"; медленный экономичный режим генерации: не меньше {G(floor.RequiredHeadroom)} ГиБ commit и {G(floor.RequiredPhysicalSafety)} ГиБ ОЗУ" : "") +
				$" (сейчас {G(commit)} ГиБ commit, {G(physical)} ГиБ ОЗУ{pf})." +
				// Presets 28.10: vanilla Large can no longer be created; name the smaller preset.
				(smallerPresetId != null
					? $" Выберите меньший пресет: {smallerPresetId} (на выделенном сервере DINGDONG_SERVER_WORLD_SIZE={smallerPresetId})" +
					  " или закройте другие приложения и повторите."
					: " Закройте другие приложения и повторите.");
		}
		string firstEn = physicalShort
			? $"Continental world requires {G(floor.RequiredPhysicalSafety)} GiB available physical memory; available={G(physical)} GiB."
			: $"Continental world requires {G(floor.RequiredHeadroom)} GiB available commit; available={G(commit)} GiB.";
		string pfEn = pagefile.Known ? $"; the Windows page file can still grow by about {G(pagefile.GrowthBytes)} GiB" : "";
		return firstEn +
			$" Normal {presetLabel} mode needs {G(fast.RequiredHeadroom)} GiB commit and {G(fast.RequiredPhysicalSafety)} GiB free RAM" +
			(lowTier ? $"; the slower low-memory generation mode needs at least {G(floor.RequiredHeadroom)} GiB commit and {G(floor.RequiredPhysicalSafety)} GiB free RAM" : "") +
			$" (now {G(commit)} GiB commit, {G(physical)} GiB free RAM{pfEn})." +
			// Presets 28.10: vanilla Large can no longer be created; name the smaller preset.
			(smallerPresetId != null
				? $" Choose the smaller preset {smallerPresetId} (dedicated server: DINGDONG_SERVER_WORLD_SIZE={smallerPresetId})" +
				  " or close other applications and retry."
				: " Close other applications and retry.");
	}

	// Read-only: HKLM\...\Memory Management\PagingFiles plus drive sizes. Never changes settings.
	internal static RamOptPagefileEstimate EstimatePagefileGrowth(ContinentalMemorySnapshot memory)
	{
		if (!OperatingSystem.IsWindows())
			return new(false, 0, "not-windows");
		try
		{
			using RegistryKey key = Registry.LocalMachine.OpenSubKey(
				@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management");
			// REG_MULTI_SZ on most systems; measured REG_SZ ("?:\pagefile.sys") on the owner's PC.
			object value = key?.GetValue("PagingFiles");
			string[] entries = value as string[] ?? (value is string single ? new[] { single } : null);
			if (entries == null || entries.Length == 0)
				return new(false, 0, "no-paging-files");
			long ram = memory.TotalPhysicalBytes;
			// Commit limit = RAM + all page files (Microsoft Learn, page file sizing).
			long current = Math.Max(0, memory.CommitLimitBytes - ram);
			long maxTotal = 0, diskRoom = 0;
			var parts = new List<string>();
			var drives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string raw in entries)
			{
				string[] tokens = (raw ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (tokens.Length == 0) continue;
				string path = tokens[0];
				bool allDrives = path.StartsWith("?:", StringComparison.Ordinal);
				string root = allDrives ? Path.GetPathRoot(Environment.SystemDirectory) : Path.GetPathRoot(path);
				if (string.IsNullOrEmpty(root)) continue;
				var drive = new DriveInfo(root);
				if (!drive.IsReady) continue;
				bool systemManaged = allDrives || tokens.Length < 3 || (tokens[1] == "0" && tokens[2] == "0");
				long maxBytes;
				// System-managed maximum: 3 x RAM or 4 GB, whichever is larger, limited to volume size / 8
				// (Microsoft Learn: "How to determine the appropriate page file size for 64-bit Windows").
				if (systemManaged)
					maxBytes = Math.Min(Math.Max(checked(3 * ram), 4L * GiB), drive.TotalSize / 8);
				else if (long.TryParse(tokens[2], out long maxMiB) && maxMiB > 0)
					maxBytes = checked(maxMiB * MiB);
				else
					continue;
				maxTotal = checked(maxTotal + maxBytes);
				if (drives.Add(root))
					diskRoom = checked(diskRoom + Math.Max(0, drive.AvailableFreeSpace - GiB));
				parts.Add($"{path}:{(systemManaged ? "system" : "custom")}:max={maxBytes}");
			}
			if (parts.Count == 0)
				return new(false, 0, "no-usable-paging-file");
			long growth = Math.Max(0, Math.Min(maxTotal - current, diskRoom));
			return new(true, growth, $"{string.Join(";", parts)};current~{current};diskRoom={diskRoom}");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or
			ArgumentException or OverflowException)
		{
			return new(false, 0, "unavailable:" + ex.GetType().Name);
		}
	}
}
