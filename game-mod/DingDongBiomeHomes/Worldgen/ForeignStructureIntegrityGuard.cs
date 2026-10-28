using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Exact five-plane readback; local lifetime, no persistent world-sized cache.
// RamOpt 28.10: x10 worlds capture ~6.7M cells (~103 MiB as 16-byte structs). In the
// economical memory mode the same five planes are streamed as 15 raw bytes per cell
// to a delete-on-close temporary file and read back in the identical order (x outer,
// y inner), so every comparison, repair and failure is exactly the same as in memory.
internal sealed class ForeignStructureIntegrityGuard : IDisposable
{
	private const int MaxRepairCells = 4096;
	private readonly List<(Rectangle Area, AuthoredTileSnapshot[] Cells)> snapshots = new();
	private long cellCount;
	// RamOpt 28.10: economical representation (null in the fast mode).
	private readonly List<Rectangle> spilledAreas = new();
	private FileStream spill;
	private long spillBytes;

	private string Mode => spill != null ? "disk-stream" : "exact";

	internal static Rectangle[] ReadReservations()
	{
		var flags = BindingFlags.NonPublic | BindingFlags.Instance;
		var map = GenVars.structures;
		var gate = typeof(StructureMap).GetField("_lock", flags)?.GetValue(map)
			?? throw new MissingFieldException("StructureMap._lock");
		lock (gate)
		{
			if (typeof(StructureMap).GetField("_protectedStructures", flags)?.GetValue(map) is not List<Rectangle> regions)
				throw new MissingFieldException("StructureMap._protectedStructures");
			return regions.ToArray();
		}
	}

	internal static ForeignStructureIntegrityGuard Capture(Mod mod)
	{
		var guard = new ForeignStructureIntegrityGuard();
		Rectangle world = new(0, 0, Main.maxTilesX, Main.maxTilesY);
		// RamOpt 28.10: same region list and order as before, computed once.
		var areas = new List<Rectangle>();
		long cells = 0;
		foreach (Rectangle region in ReadReservations().Distinct())
		{
			Rectangle area = Rectangle.Intersect(region, world);
			if (area.Width <= 0 || area.Height <= 0) continue;
			areas.Add(area);
			cells += checked(area.Width * area.Height);
		}
		long exactBytes = checked(cells * Unsafe.SizeOf<AuthoredTileSnapshot>());
		bool economical = SpillCodec.Supported &&
			RamOptMemoryMode.ForStage(mod, "foreign-integrity-capture", exactBytes);
		if (!economical || !guard.TryCaptureSpill(mod, areas, cells))
		{
			foreach (Rectangle area in areas)
			{
				var snapshot = new AuthoredTileSnapshot[checked(area.Width * area.Height)];
				for (int x = 0; x < area.Width; x++)
				for (int y = 0; y < area.Height; y++)
					snapshot[x * area.Height + y] = AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]);
				guard.snapshots.Add((area, snapshot));
				guard.cellCount += snapshot.LongLength;
			}
		}
		mod.Logger.Info($"DINGDONG_FOREIGN_INTEGRITY|state=CAPTURED|regions={guard.RegionCount}|cells={guard.cellCount}" +
			$"|mode={guard.Mode}|inMemoryBytes={(guard.spill != null ? 0 : exactBytes)}|streamedBytes={guard.spillBytes}");
		return guard;
	}

	private int RegionCount => snapshots.Count + spilledAreas.Count;

	internal void Validate(Mod mod)
	{
		try
		{
			foreach (var entry in snapshots)
			{
				Rectangle area = entry.Area;
				for (int x = 0; x < area.Width; x++)
				for (int y = 0; y < area.Height; y++)
					if (entry.Cells[x * area.Height + y] != AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]))
						throw new InvalidOperationException($"Foreign protected structure changed at {area.X + x},{area.Y + y}; generation rejected.");
			}
			if (spill != null)
			{
				var reader = new SpillReader(spill);
				foreach (Rectangle area in spilledAreas)
				for (int x = 0; x < area.Width; x++)
				for (int y = 0; y < area.Height; y++)
					if (reader.Next() != AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]))
						throw new InvalidOperationException($"Foreign protected structure changed at {area.X + x},{area.Y + y}; generation rejected.");
			}
			mod.Logger.Info($"DINGDONG_FOREIGN_INTEGRITY|verdict=PASS|regions={RegionCount}|cells={cellCount}|planes=5|changed=0|mode={Mode}");
		}
		finally
		{
			// RamOpt 28.10: the snapshot ends here in every outcome; give the memory back.
			snapshots.Clear();
			snapshots.TrimExcess();
			Dispose();
		}
	}

	internal void RepairAndValidate(Mod mod)
	{
		// Native tile framing may cascade into a provider's protected area even
		// after the authored write envelope was admitted. Restore only the exact
		// five planes captured immediately before our own placement batch.
		var repairs = new Dictionary<(int X, int Y), AuthoredTileSnapshot>();
		try
		{
			foreach (var entry in snapshots)
			{
				Rectangle area = entry.Area;
				for (int x = 0; x < area.Width; x++)
				for (int y = 0; y < area.Height; y++)
					CollectRepair(repairs, area.X + x, area.Y + y, entry.Cells[x * area.Height + y]);
			}
			if (spill != null)
			{
				var reader = new SpillReader(spill);
				foreach (Rectangle area in spilledAreas)
				for (int x = 0; x < area.Width; x++)
				for (int y = 0; y < area.Height; y++)
					CollectRepair(repairs, area.X + x, area.Y + y, reader.Next());
			}
		}
		catch
		{
			Dispose();
			throw;
		}
		foreach (var repair in repairs)
			repair.Value.Restore(Main.tile[repair.Key.X, repair.Key.Y]);
		foreach (var repair in repairs)
			if (repair.Value != AuthoredTileSnapshot.Read(Main.tile[repair.Key.X, repair.Key.Y]))
			{
				Dispose();
				throw new InvalidOperationException($"Foreign protected structure repair failed at {repair.Key.X},{repair.Key.Y}; generation rejected.");
			}
		if (repairs.Count > 0)
			mod.Logger.Warn($"DINGDONG_FOREIGN_INTEGRITY|verdict=REPAIRED|cells={repairs.Count}|limit={MaxRepairCells}|planes=5");
		Validate(mod);
	}

	private static void CollectRepair(Dictionary<(int X, int Y), AuthoredTileSnapshot> repairs,
		int worldX, int worldY, AuthoredTileSnapshot expected)
	{
		if (expected == AuthoredTileSnapshot.Read(Main.tile[worldX, worldY])) return;
		repairs.TryAdd((worldX, worldY), expected);
		if (repairs.Count > MaxRepairCells)
			throw new InvalidOperationException($"Foreign protected structure changed in more than {MaxRepairCells} cells; generation rejected without repair.");
	}

	public void Dispose()
	{
		spill?.Dispose();
		spill = null;
	}

	// RamOpt 28.10: stream the five planes of every region to a temporary file.
	private bool TryCaptureSpill(Mod mod, List<Rectangle> areas, long cells)
	{
		long bytes = checked(cells * SpillCodec.CellBytes);
		string path = null;
		try
		{
			string directory = Path.GetTempPath();
			var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(directory)));
			const long margin = 512L * 1024 * 1024;
			if (drive.AvailableFreeSpace < checked(bytes + margin))
			{
				mod.Logger.Warn($"DINGDONG_FOREIGN_INTEGRITY|state=STREAM_SKIPPED|reason=disk-space|needed={bytes}|free={drive.AvailableFreeSpace}|fallback=exact");
				return false;
			}
			path = Path.Combine(directory, $"DingDongForeignGuard-{Environment.ProcessId}-{Guid.NewGuid():N}.tmp");
			spill = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
				SpillCodec.BufferBytes, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
			byte[] buffer = new byte[SpillCodec.BufferBytes];
			int used = 0;
			foreach (Rectangle area in areas)
			{
				for (int x = 0; x < area.Width; x++)
				for (int y = 0; y < area.Height; y++)
				{
					if (used + SpillCodec.CellBytes > buffer.Length)
					{
						spill.Write(buffer, 0, used);
						used = 0;
					}
					SpillCodec.Encode(AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]),
						buffer.AsSpan(used, SpillCodec.CellBytes));
					used += SpillCodec.CellBytes;
				}
				spilledAreas.Add(area);
				cellCount += (long)area.Width * area.Height;
			}
			if (used > 0) spill.Write(buffer, 0, used);
			spill.Flush();
			spillBytes = spill.Length;
			if (spillBytes != bytes)
				throw new IOException($"Integrity stream length {spillBytes} != {bytes}.");
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
			System.Security.SecurityException or ArgumentException or NotSupportedException)
		{
			// The capture only reads tiles: falling back to the exact in-memory copy is identical.
			Dispose();
			spilledAreas.Clear();
			cellCount = 0;
			spillBytes = 0;
			mod.Logger.Warn($"DINGDONG_FOREIGN_INTEGRITY|state=STREAM_FAILED|reason={ex.GetType().Name}|path={path}|fallback=exact");
			return false;
		}
	}

	private sealed class SpillReader
	{
		private readonly FileStream stream;
		private readonly byte[] buffer = new byte[SpillCodec.BufferBytes];
		private int position, length;

		internal SpillReader(FileStream stream)
		{
			this.stream = stream;
			stream.Seek(0, SeekOrigin.Begin);
		}

		internal AuthoredTileSnapshot Next()
		{
			if (position + SpillCodec.CellBytes > length)
			{
				int rest = length - position;
				if (rest > 0) Buffer.BlockCopy(buffer, position, buffer, 0, rest);
				length = rest;
				position = 0;
				int read;
				while (length < SpillCodec.CellBytes &&
					(read = stream.Read(buffer, length, buffer.Length - length)) > 0)
					length += read;
				if (length < SpillCodec.CellBytes)
					throw new InvalidOperationException("Foreign integrity stream ended early; generation rejected.");
			}
			AuthoredTileSnapshot cell = SpillCodec.Decode(buffer.AsSpan(position, SpillCodec.CellBytes));
			position += SpillCodec.CellBytes;
			return cell;
		}
	}

	private static class SpillCodec
	{
		internal const int CellBytes = 15;
		// Multiple of 15 so a cell never straddles two buffer refills when writing.
		internal const int BufferBytes = 15 * 4369;
		// Verified against tModLoader 1.4.4 TileData.Default.cs: 2 + 2 + 2 + 1 + 8 bytes.
		internal static readonly bool Supported =
			Unsafe.SizeOf<TileTypeData>() == 2 && Unsafe.SizeOf<WallTypeData>() == 2 &&
			Unsafe.SizeOf<LiquidData>() == 2 && Unsafe.SizeOf<TileWallBrightnessInvisibilityData>() == 1 &&
			Unsafe.SizeOf<TileWallWireStateData>() == 8;

		internal static void Encode(in AuthoredTileSnapshot cell, Span<byte> target)
		{
			ref byte start = ref MemoryMarshal.GetReference(target);
			Unsafe.WriteUnaligned(ref start, cell.TileType);
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref start, 2), cell.WallType);
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref start, 4), cell.Liquid);
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref start, 6), cell.Brightness);
			Unsafe.WriteUnaligned(ref Unsafe.Add(ref start, 7), cell.State);
		}

		internal static AuthoredTileSnapshot Decode(ReadOnlySpan<byte> source)
		{
			ref byte start = ref MemoryMarshal.GetReference(source);
			return new(
				Unsafe.ReadUnaligned<TileTypeData>(ref start),
				Unsafe.ReadUnaligned<WallTypeData>(ref Unsafe.Add(ref start, 2)),
				Unsafe.ReadUnaligned<LiquidData>(ref Unsafe.Add(ref start, 4)),
				Unsafe.ReadUnaligned<TileWallBrightnessInvisibilityData>(ref Unsafe.Add(ref start, 6)),
				Unsafe.ReadUnaligned<TileWallWireStateData>(ref Unsafe.Add(ref start, 7)));
		}
	}
}
