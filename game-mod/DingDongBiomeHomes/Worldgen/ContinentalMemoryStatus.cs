using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalMemorySnapshot(
	long TotalPhysicalBytes,
	long AvailablePhysicalBytes,
	long AvailableCommitBytes,
	long CommitLimitBytes = 0); // RamOpt 28.10: ullTotalPageFile (RAM + page files)

internal static class ContinentalMemoryStatus
{
	internal static ContinentalMemorySnapshot Read()
	{
		if (!OperatingSystem.IsWindows())
			throw new PlatformNotSupportedException(
				"Continental memory admission requires authoritative Windows memory telemetry.");

		MemoryStatusEx status = new();
		status.Length = checked((uint)Marshal.SizeOf<MemoryStatusEx>());
		if (!GlobalMemoryStatusEx(ref status))
			throw new Win32Exception(Marshal.GetLastWin32Error(),
				"GlobalMemoryStatusEx failed during continental memory admission.");

		long total = CheckedPositive(status.TotalPhysical, "total physical memory");
		long physical = CheckedPositive(status.AvailablePhysical, "available physical memory");
		long commit = CheckedPositive(status.AvailablePageFile, "available commit");
		long limit = status.TotalPageFile > long.MaxValue ? long.MaxValue : (long)status.TotalPageFile;
		return Simulated(new(total, physical, commit, limit));
	}

	// RamOpt 28.10: test hook DINGDONG_RAMOPT_SIMULATE_AVAILABLE=physicalBytes,commitBytes.
	// It can only LOWER the measured values, so it never weakens admission.
	private static ContinentalMemorySnapshot Simulated(ContinentalMemorySnapshot real)
	{
		string value = Environment.GetEnvironmentVariable("DINGDONG_RAMOPT_SIMULATE_AVAILABLE");
		if (string.IsNullOrWhiteSpace(value)) return real;
		string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
		if (parts.Length != 2 || !long.TryParse(parts[0], out long physical) || !long.TryParse(parts[1], out long commit) ||
			physical <= 0 || commit <= 0)
			throw new ContinentalMemoryAdmissionException("DINGDONG_RAMOPT_SIMULATE_AVAILABLE must be physicalBytes,commitBytes.");
		return real with
		{
			AvailablePhysicalBytes = Math.Min(real.AvailablePhysicalBytes, physical),
			AvailableCommitBytes = Math.Min(real.AvailableCommitBytes, commit)
		};
	}

	private static long CheckedPositive(ulong value, string field)
	{
		if (value == 0 || value > long.MaxValue)
			throw new ContinentalMemoryAdmissionException(
				$"Continental memory admission could not establish {field}.");
		return checked((long)value);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

	[StructLayout(LayoutKind.Sequential)]
	private struct MemoryStatusEx
	{
		internal uint Length;
		private uint MemoryLoad;
		internal ulong TotalPhysical;
		internal ulong AvailablePhysical;
		internal ulong TotalPageFile;
		internal ulong AvailablePageFile;
		private ulong TotalVirtual;
		private ulong AvailableVirtual;
		private ulong AvailableExtendedVirtual;
	}
}
