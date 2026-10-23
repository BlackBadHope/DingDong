using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalMemorySnapshot(
	long TotalPhysicalBytes,
	long AvailablePhysicalBytes,
	long AvailableCommitBytes);

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
		return new(total, physical, commit);
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
		private ulong TotalPageFile;
		internal ulong AvailablePageFile;
		private ulong TotalVirtual;
		private ulong AvailableVirtual;
		private ulong AvailableExtendedVirtual;
	}
}
