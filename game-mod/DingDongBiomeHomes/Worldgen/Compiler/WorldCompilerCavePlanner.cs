using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerCavePlanner
{
	private const int HorizontalSpacing = 320;
	private const int VerticalSpacing = 300;

	internal static WorldCompilerCavePlan Create(int width, int height, ulong seed,
		WorldCompilerSurfaceProfile surface)
	{
		int marginX = Math.Max(320, width / 70);
		int minimumY = height * 30 / 100;
		int maximumY = height - Math.Max(700, height / 10);
		int columns = Math.Max(2, (width - marginX * 2) / HorizontalSpacing);
		int rows = Math.Max(2, (maximumY - minimumY) / VerticalSpacing);
		var nodes = new WorldCompilerCaveNode[checked(columns * rows)];
		for (int row = 0; row < rows; row++)
		for (int column = 0; column < columns; column++)
		{
			int id = row * columns + column;
			int x = marginX + HorizontalSpacing / 2 + column * HorizontalSpacing;
			x += WorldCompilerSeed.Range(seed, id, 101, -78, 79);
			x = Math.Clamp(x, marginX, width - marginX - 1);
			int y = minimumY + VerticalSpacing / 2 + row * VerticalSpacing;
			y += WorldCompilerSeed.Range(seed, id, 103, -68, 69);
			y = Math.Max(y, surface[x] + Math.Max(360, height / 18));
			y = Math.Clamp(y, minimumY, maximumY);
			int radius = WorldCompilerSeed.Range(seed, id, 107, 8, 21);
			nodes[id] = new WorldCompilerCaveNode(id, x, y, radius);
		}
		WorldCompilerCaveEdge[] edges = Connect(nodes, columns, rows, seed);
		return new WorldCompilerCavePlan(nodes, edges, columns, rows,
			Fingerprint(nodes, edges));
	}

	private static WorldCompilerCaveEdge[] Connect(WorldCompilerCaveNode[] nodes,
		int columns, int rows, ulong seed)
	{
		var edges = new List<WorldCompilerCaveEdge>(nodes.Length * 2);
		for (int row = 0; row < rows; row++)
		{
			bool reverse = (row & 1) != 0;
			for (int step = 1; step < columns; step++)
			{
				int previous = row * columns + (reverse ? columns - step : step - 1);
				int current = row * columns + (reverse ? columns - step - 1 : step);
				Add(edges, nodes, previous, current);
			}
			if (row + 1 < rows)
			{
				int endColumn = reverse ? 0 : columns - 1;
				Add(edges, nodes, row * columns + endColumn,
					(row + 1) * columns + endColumn);
			}
		}
		for (int row = 0; row + 1 < rows; row++)
		for (int column = 0; column < columns; column++)
		{
			int from = row * columns + column;
			if (WorldCompilerSeed.At(seed, from, 109) % 5UL == 0)
				Add(edges, nodes, from, from + columns);
		}
		return edges.ToArray();
	}

	private static void Add(List<WorldCompilerCaveEdge> edges,
		WorldCompilerCaveNode[] nodes, int from, int to)
	{
		int radius = Math.Max(5, (nodes[from].Radius + nodes[to].Radius) / 3);
		edges.Add(new WorldCompilerCaveEdge(from, to, radius));
	}

	private static ulong Fingerprint(WorldCompilerCaveNode[] nodes,
		WorldCompilerCaveEdge[] edges)
	{
		ulong hash = 0xCBF29CE484222325UL;
		foreach (WorldCompilerCaveNode node in nodes)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)node.X << 32 ^
				(uint)node.Y ^ (uint)node.Radius);
		foreach (WorldCompilerCaveEdge edge in edges)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)edge.From << 32 ^
				(uint)edge.To ^ (uint)edge.Radius);
		return hash;
	}
}
