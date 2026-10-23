using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerSpatialIndex
{
	private readonly int cellSize;
	private readonly Dictionary<long, List<WorldCompilerRect>> cells = [];
	internal int ReservationCount { get; private set; }

	internal WorldCompilerSpatialIndex(int cellSize = 128)
	{
		if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
		this.cellSize = cellSize;
	}

	internal bool TryReserve(WorldCompilerRect area, int padding = 0)
	{
		if (padding < 0) throw new ArgumentOutOfRangeException(nameof(padding));
		WorldCompilerRect query = area.Inflate(padding);
		if (Overlaps(query)) return false;
		ReserveExisting(query);
		return true;
	}

	// Existing obstacles form a union: overlap must not discard the new footprint.
	internal void ReserveExisting(WorldCompilerRect query)
	{
		ForCells(query, (key) =>
		{
			if (!cells.TryGetValue(key, out List<WorldCompilerRect> bucket))
				cells[key] = bucket = [];
			bucket.Add(query);
		});
		ReservationCount++;
	}

	internal bool Overlaps(WorldCompilerRect query)
	{
		int left = FloorDiv(query.X, cellSize), right = FloorDiv(query.Right - 1, cellSize);
		int top = FloorDiv(query.Y, cellSize), bottom = FloorDiv(query.Bottom - 1, cellSize);
		for (int x = left; x <= right; x++)
		for (int y = top; y <= bottom; y++)
		{
			if (!cells.TryGetValue(((long)x << 32) ^ (uint)y, out List<WorldCompilerRect> bucket))
				continue;
			foreach (WorldCompilerRect occupied in bucket)
				if (occupied.Intersects(query)) return true;
		}
		return false;
	}

	private void ForCells(WorldCompilerRect area, Action<long> visit)
	{
		int left = FloorDiv(area.X, cellSize);
		int right = FloorDiv(area.Right - 1, cellSize);
		int top = FloorDiv(area.Y, cellSize);
		int bottom = FloorDiv(area.Bottom - 1, cellSize);
		for (int x = left; x <= right; x++)
		for (int y = top; y <= bottom; y++)
			visit(((long)x << 32) ^ (uint)y);
	}

	private static int FloorDiv(int value, int divisor) => value >= 0 ?
		value / divisor : -((-value + divisor - 1) / divisor);
}
