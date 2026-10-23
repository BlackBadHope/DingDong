using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen;

// Broad phase only: the vanilla +/-10 predicate and all field arithmetic remain authoritative.
internal sealed class DesertClusterSpatialIndex
{
	private const double CellSize = 10;
	private readonly Dictionary<(int X, int Y), int[]> cells = new();
	private readonly int count;

	internal DesertClusterSpatialIndex(IReadOnlyList<(double X, double Y)> anchors)
	{
		count = anchors.Count;
		Dictionary<(int, int), List<int>> building = new();
		for (int i = 0; i < count; i++)
		{
			(int x, int y) = Cell(anchors[i].X, anchors[i].Y);
			// Conservative extra cell handles rounding at exact +/-10 boundaries.
			for (int dx = -2; dx <= 2; dx++)
			for (int dy = -2; dy <= 2; dy++)
			{
				var key = (x + dx, y + dy);
				if (!building.TryGetValue(key, out var list)) building[key] = list = new();
				list.Add(i); // Input order is the original cluster ID / tie-break order.
			}
		}
		foreach (var pair in building) cells.Add(pair.Key, pair.Value.ToArray());
	}

	internal int Next(double x, double y, int first)
	{
		if (!cells.TryGetValue(Cell(x, y), out int[] candidates)) return count;
		int index = Array.BinarySearch(candidates, first);
		if (index < 0) index = ~index;
		return index < candidates.Length ? candidates[index] : count;
	}

	private static (int X, int Y) Cell(double x, double y)
	{
		if (!double.IsFinite(x) || !double.IsFinite(y) ||
			Math.Abs(x) > 1_000_000 || Math.Abs(y) > 1_000_000)
			throw new ArgumentOutOfRangeException(nameof(x), "Non-world desert cluster coordinate.");
		return ((int)Math.Floor(x / CellSize), (int)Math.Floor(y / CellSize));
	}
}
