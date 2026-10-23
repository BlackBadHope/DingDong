using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

// Exact five-plane readback; local lifetime, no persistent world-sized cache.
internal sealed class ForeignStructureIntegrityGuard
{
	private const int MaxRepairCells = 4096;
	private readonly List<(Rectangle Area, AuthoredTileSnapshot[] Cells)> snapshots = new();
	private long cellCount;

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
		foreach (Rectangle region in ReadReservations().Distinct())
		{
			Rectangle area = Rectangle.Intersect(region, world);
			if (area.Width <= 0 || area.Height <= 0) continue;
			var cells = new AuthoredTileSnapshot[checked(area.Width * area.Height)];
			for (int x = 0; x < area.Width; x++)
			for (int y = 0; y < area.Height; y++)
				cells[x * area.Height + y] = AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]);
			guard.snapshots.Add((area, cells));
			guard.cellCount += cells.LongLength;
		}
		mod.Logger.Info($"DINGDONG_FOREIGN_INTEGRITY|state=CAPTURED|regions={guard.snapshots.Count}|cells={guard.cellCount}");
		return guard;
	}

	internal void Validate(Mod mod)
	{
		foreach (var entry in snapshots)
		{
			Rectangle area = entry.Area;
			for (int x = 0; x < area.Width; x++)
			for (int y = 0; y < area.Height; y++)
				if (entry.Cells[x * area.Height + y] != AuthoredTileSnapshot.Read(Main.tile[area.X + x, area.Y + y]))
					throw new InvalidOperationException($"Foreign protected structure changed at {area.X + x},{area.Y + y}; generation rejected.");
		}
		mod.Logger.Info($"DINGDONG_FOREIGN_INTEGRITY|verdict=PASS|regions={snapshots.Count}|cells={cellCount}|planes=5|changed=0");
		snapshots.Clear();
	}

	internal void RepairAndValidate(Mod mod)
	{
		// Native tile framing may cascade into a provider's protected area even
		// after the authored write envelope was admitted. Restore only the exact
		// five planes captured immediately before our own placement batch.
		var repairs = new Dictionary<(int X, int Y), AuthoredTileSnapshot>();
		foreach (var entry in snapshots)
		{
			Rectangle area = entry.Area;
			for (int x = 0; x < area.Width; x++)
			for (int y = 0; y < area.Height; y++)
			{
				int worldX = area.X + x, worldY = area.Y + y;
				AuthoredTileSnapshot expected = entry.Cells[x * area.Height + y];
				if (expected == AuthoredTileSnapshot.Read(Main.tile[worldX, worldY])) continue;
				repairs.TryAdd((worldX, worldY), expected);
				if (repairs.Count > MaxRepairCells)
					throw new InvalidOperationException($"Foreign protected structure changed in more than {MaxRepairCells} cells; generation rejected without repair.");
			}
		}
		foreach (var repair in repairs)
			repair.Value.Restore(Main.tile[repair.Key.X, repair.Key.Y]);
		foreach (var repair in repairs)
			if (repair.Value != AuthoredTileSnapshot.Read(Main.tile[repair.Key.X, repair.Key.Y]))
				throw new InvalidOperationException($"Foreign protected structure repair failed at {repair.Key.X},{repair.Key.Y}; generation rejected.");
		if (repairs.Count > 0)
			mod.Logger.Warn($"DINGDONG_FOREIGN_INTEGRITY|verdict=REPAIRED|cells={repairs.Count}|limit={MaxRepairCells}|planes=5");
		Validate(mod);
	}
}
