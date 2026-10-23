using System;

namespace DingDongBiomeHomes.Worldgen;

// A failed enqueue retains a bounded, conservative recheck obligation. No fluid
// is removed and no unbounded per-cell overflow collection is allocated.
internal sealed class ContinentalLiquidOverflow
{
	internal const int ChunkSize = 32;
	private readonly int width, height, columns;
	private readonly uint[] generation, scanGeneration;
	private readonly int[] cursor;
	private readonly ContinentalLiquidQueue chunks;
	internal int Count => chunks.Count;
	internal int Capacity => cursor.Length;

	internal ContinentalLiquidOverflow(int width, int height, int laneCount)
	{
		if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
		this.width = width; this.height = height;
		columns = (width + ChunkSize - 1) / ChunkSize;
		int count = checked(columns * ((height + ChunkSize - 1) / ChunkSize));
		generation = new uint[count]; scanGeneration = new uint[count]; cursor = new int[count];
		chunks = new ContinentalLiquidQueue(laneCount, count);
	}

	internal void Mark(int x, int y, int lane)
	{
		if ((uint)x >= (uint)width || (uint)y >= (uint)height) return;
		int key = x / ChunkSize + (y / ChunkSize) * columns;
		// Equality only: unsigned wrap cannot drop a mark on an ordinary pass.
		generation[key]++;
		if (chunks.Contains(key)) return;
		cursor[key] = 0;
		chunks.Add(key, key, lane);
	}

	internal bool TryNext(out int x, out int y)
	{
		if (!chunks.TryNext(out int key, out _)) { x = y = -1; return false; }
		if (cursor[key] == 0) scanGeneration[key] = generation[key];
		int local = cursor[key]++;
		x = key % columns * ChunkSize + local % ChunkSize;
		y = key / columns * ChunkSize + local / ChunkSize;
		if (cursor[key] == ChunkSize * ChunkSize)
		{
			cursor[key] = 0;
			if (generation[key] == scanGeneration[key]) chunks.Remove(key);
			else chunks.RotateToEnd(key); // A perpetually hot chunk cannot pin its lane.
		}
		// Edge padding is harmless to the scan, but must never be read as a tile.
		if (x >= width || y >= height) { x = y = -1; }
		return true;
	}

	internal void Clear()
	{
		chunks.Clear();
		Array.Clear(generation); Array.Clear(scanGeneration); Array.Clear(cursor);
	}
}
