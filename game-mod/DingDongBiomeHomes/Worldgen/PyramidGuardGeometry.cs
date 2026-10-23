using System;
using System.Collections.Generic;
using System.Linq;

namespace DingDongBiomeHomes.Worldgen;

// Filled row hulls, not a sparse tile mask: rooms and their air remain guarded.
internal static class PyramidGuardGeometry
{
	internal readonly record struct Bounds(int X, int Y, int Width, int Height);
	internal const int HorizontalPadding = 24;
	internal const int TopPadding = 32;
	internal const int BottomPadding = 24;

	internal static List<Bounds> Build(IEnumerable<(int X, int Y)> occupied,
		Bounds gate, int worldWidth, int worldHeight)
	{
		if (worldWidth < 3 || worldHeight < 3 || gate.Width <= 0 || gate.Height <= 0)
			throw new ArgumentOutOfRangeException(nameof(gate));
		SortedDictionary<int, (int Left, int Right)> rows = new();
		foreach ((int x, int y) in occupied)
		{
			if (x < 1 || y < 1 || x >= worldWidth - 1 || y >= worldHeight - 1)
				throw new ArgumentOutOfRangeException(nameof(occupied));
			Include(rows, y, x, x);
		}
		if (rows.Count == 0) throw new ArgumentException("Pyramid has no occupied rows.", nameof(occupied));
		int[] originalRows = rows.Keys.ToArray();
		// An entirely empty chamber row must not create a cut through the hull.
		for (int i = 1; i < originalRows.Length; i++)
		{
			int before = originalRows[i - 1], after = originalRows[i];
			var a = rows[before]; var b = rows[after];
			for (int y = before + 1; y < after; y++)
				rows[y] = (Math.Min(a.Left, b.Left), Math.Max(a.Right, b.Right));
		}

		SortedDictionary<int, (int Left, int Right)> padded = new();
		foreach (var row in rows)
		{
			int left = Math.Max(1, row.Value.Left - HorizontalPadding);
			int right = Math.Min(worldWidth - 2, row.Value.Right + HorizontalPadding);
			for (int y = Math.Max(1, row.Key - TopPadding);
				y <= Math.Min(worldHeight - 2, row.Key + BottomPadding); y++)
				Include(padded, y, left, right);
		}

		// Retain an uninterrupted full-width approach above the entire gate up
		// to the old guard's top, not just the individual gate tile positions.
		int approachLeft = Math.Max(1, gate.X - HorizontalPadding);
		int approachRight = Math.Min(worldWidth - 2, gate.X + gate.Width - 1 + HorizontalPadding);
		for (int y = Math.Max(1, originalRows[0] - TopPadding);
			y <= Math.Min(worldHeight - 2, gate.Y + gate.Height - 1 + BottomPadding); y++)
			Include(padded, y, approachLeft, approachRight);

		List<Bounds> result = new();
		int runTop = -1, runBottom = -1, runLeft = 0, runRight = 0;
		foreach (var row in padded)
		{
			if (runTop >= 0 && row.Key == runBottom + 1 && row.Value.Left == runLeft && row.Value.Right == runRight)
			{
				runBottom = row.Key;
				continue;
			}
			if (runTop >= 0) result.Add(new(runLeft, runTop, runRight - runLeft + 1, runBottom - runTop + 1));
			runTop = runBottom = row.Key;
			runLeft = row.Value.Left; runRight = row.Value.Right;
		}
		if (runTop >= 0) result.Add(new(runLeft, runTop, runRight - runLeft + 1, runBottom - runTop + 1));
		return result;
	}

	private static void Include(SortedDictionary<int, (int Left, int Right)> rows, int y, int left, int right)
	{
		if (rows.TryGetValue(y, out var prior))
			rows[y] = (Math.Min(prior.Left, left), Math.Max(prior.Right, right));
		else rows[y] = (left, right);
	}
}
