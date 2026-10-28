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
		// S12 28.10: a cell whose only difference is its sprite frame (a neighbour was
		// re-framed) is restored without counting toward the limit: putting back the exact
		// captured frame cannot damage the provider's structure. Real content changes
		// (tile, wall, liquid, paint, slope, wires) still reject the world above the limit.
		var repairs = new Dictionary<(int X, int Y), AuthoredTileSnapshot>();
		int contentRepairs = 0;
		try
		{
			ForEachCell((x, y, expected) => CollectRepair(repairs, ref contentRepairs, x, y, expected));
		}
		catch (InvalidOperationException) when (contentRepairs > MaxRepairCells)
		{
			Census(mod, "rejected");
			Dispose();
			throw;
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
			mod.Logger.Warn($"DINGDONG_FOREIGN_INTEGRITY|verdict=REPAIRED|cells={repairs.Count}|contentCells={contentRepairs}|frameOnlyCells={repairs.Count - contentRepairs}|limit={MaxRepairCells}|planes=5");
		Validate(mod);
	}

	private static void CollectRepair(Dictionary<(int X, int Y), AuthoredTileSnapshot> repairs, ref int contentRepairs,
		int worldX, int worldY, AuthoredTileSnapshot expected)
	{
		AuthoredTileSnapshot current = AuthoredTileSnapshot.Read(Main.tile[worldX, worldY]);
		if (expected == current) return;
		if (!repairs.TryAdd((worldX, worldY), expected) || FrameOnly(expected, current)) return;
		if (++contentRepairs > MaxRepairCells)
			throw new InvalidOperationException($"Foreign protected structure changed in more than {MaxRepairCells} cells; generation rejected without repair.");
	}

	// S12 28.10: true when two snapshots differ only in tile/wall sprite framing.
	private static bool FrameOnly(in AuthoredTileSnapshot expected, in AuthoredTileSnapshot current)
	{
		if (!expected.TileType.Equals(current.TileType) || !expected.WallType.Equals(current.WallType) ||
			!expected.Liquid.Equals(current.Liquid) || !expected.Brightness.Equals(current.Brightness))
			return false;
		TileWallWireStateData state = expected.State;
		TileWallWireStateData now = current.State;
		state.TileFrameX = now.TileFrameX;
		state.TileFrameY = now.TileFrameY;
		state.TileFrameNumber = now.TileFrameNumber;
		state.WallFrameX = now.WallFrameX;
		state.WallFrameY = now.WallFrameY;
		state.WallFrameNumber = now.WallFrameNumber;
		return state.Equals(now);
	}

	private void ForEachCell(Action<int, int, AuthoredTileSnapshot> visit)
	{
		foreach (var entry in snapshots)
		{
			Rectangle area = entry.Area;
			for (int x = 0; x < area.Width; x++)
			for (int y = 0; y < area.Height; y++)
				visit(area.X + x, area.Y + y, entry.Cells[x * area.Height + y]);
		}
		if (spill != null)
		{
			var reader = new SpillReader(spill);
			foreach (Rectangle area in spilledAreas)
			for (int x = 0; x < area.Width; x++)
			for (int y = 0; y < area.Height; y++)
				visit(area.X + x, area.Y + y, reader.Next());
		}
	}

	// S12 28.10: read-only census of what changed inside foreign protected regions since the
	// capture: totals per plane and the regions with the most changed cells. Never repairs.
	internal void Census(Mod mod, string stage)
	{
		if (spill == null && snapshots.Count == 0) return;
		long changed = 0, frameOnly = 0, tile = 0, wall = 0, liquid = 0, brightness = 0, state = 0;
		var perRegion = new Dictionary<Rectangle, (int Count, int MinX, int MinY, int MaxX, int MaxY, string Example)>();
		var areaOf = new List<Rectangle>();
		foreach (var entry in snapshots) areaOf.Add(entry.Area);
		areaOf.AddRange(spilledAreas);
		int areaIndex = 0, cellInArea = 0;
		ForEachCell((x, y, expected) =>
		{
			Rectangle area = areaOf[areaIndex];
			if (++cellInArea == area.Width * area.Height) { areaIndex++; cellInArea = 0; }
			AuthoredTileSnapshot current = AuthoredTileSnapshot.Read(Main.tile[x, y]);
			if (expected == current) return;
			changed++;
			if (FrameOnly(expected, current)) { frameOnly++; return; }
			if (!expected.TileType.Equals(current.TileType)) tile++;
			if (!expected.WallType.Equals(current.WallType)) wall++;
			if (!expected.Liquid.Equals(current.Liquid)) liquid++;
			if (!expected.Brightness.Equals(current.Brightness)) brightness++;
			if (!expected.State.Equals(current.State)) state++;
			perRegion.TryGetValue(area, out var row);
			string example = row.Example ?? $"{x}:{y}:type{expected.TileType.Type}->{current.TileType.Type}:wall{expected.WallType.Type}->{current.WallType.Type}" +
				$":active{expected.State.HasTile}->{current.State.HasTile}:liquid{expected.Liquid.Amount}->{current.Liquid.Amount}";
			perRegion[area] = row.Count == 0
				? (1, x, y, x, y, example)
				: (row.Count + 1, Math.Min(row.MinX, x), Math.Min(row.MinY, y), Math.Max(row.MaxX, x), Math.Max(row.MaxY, y), example);
		});
		mod.Logger.Info($"DINGDONG_FOREIGN_CENSUS|stage={stage}|changed={changed}|frameOnly={frameOnly}|content={changed - frameOnly}" +
			$"|tileType={tile}|wall={wall}|liquid={liquid}|brightness={brightness}|stateNonFrame={state}|regionsTouched={perRegion.Count}");
		foreach (var row in perRegion.OrderByDescending(r => r.Value.Count).Take(12))
			mod.Logger.Info($"DINGDONG_FOREIGN_CENSUS_REGION|stage={stage}|region={row.Key.X},{row.Key.Y},{row.Key.Width},{row.Key.Height}" +
				$"|content={row.Value.Count}|changedBox={row.Value.MinX},{row.Value.MinY}..{row.Value.MaxX},{row.Value.MaxY}|example={row.Value.Example}");
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
