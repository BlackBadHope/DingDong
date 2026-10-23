using System;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal sealed class WorldCompilerRuntimeWorkIndex
{
	internal int[][] CaveEdges { get; }
	internal int[][] CaveNodes { get; }
	internal int[][] Basins { get; }
	internal int[][] Deposits { get; }
	internal int[][] Corridors { get; }
	internal int[][] Rooms { get; }
	internal long PayloadBytes { get; }

	private WorldCompilerRuntimeWorkIndex(int[][] caveEdges, int[][] caveNodes,
		int[][] basins, int[][] deposits, int[][] corridors, int[][] rooms)
	{
		CaveEdges = caveEdges;
		CaveNodes = caveNodes;
		Basins = basins;
		Deposits = deposits;
		Corridors = corridors;
		Rooms = rooms;
		PayloadBytes = 4L * (Count(caveEdges) + Count(caveNodes) +
			Count(basins) + Count(deposits) + Count(corridors) + Count(rooms));
	}

	internal static WorldCompilerRuntimeWorkIndex BuildCaves(WorldCompilerPlan plan) =>
		new(
			Index(plan.Chunks, plan.Caves.Edges.Length, edgeIndex =>
			{
				WorldCompilerCaveEdge edge = plan.Caves.Edges[edgeIndex];
				WorldCompilerCaveNode from = plan.Caves.Nodes[edge.From];
				WorldCompilerCaveNode to = plan.Caves.Nodes[edge.To];
				return (Math.Min(from.X, to.X) - edge.Radius,
					Math.Max(from.X, to.X) + edge.Radius + 1);
			}),
			Index(plan.Chunks, plan.Caves.Nodes.Length, nodeIndex =>
			{
				WorldCompilerCaveNode node = plan.Caves.Nodes[nodeIndex];
				return (node.X - node.Radius, node.X + node.Radius + 1);
			}), Empty, Empty, Empty, Empty);

	internal static WorldCompilerRuntimeWorkIndex BuildHydrology(
		WorldCompilerPlan plan) => new(Empty, Empty,
			Index(plan.Chunks, plan.Hydrology.Basins.Length, basinIndex =>
			{
				WorldCompilerBasin basin = plan.Hydrology.Basins[basinIndex];
				return (basin.X - basin.RadiusX,
					basin.X + basin.RadiusX + 1);
			}), Empty, Empty, Empty);

	internal static WorldCompilerRuntimeWorkIndex BuildResources(
		WorldCompilerPlan plan) => new(Empty, Empty, Empty,
			Index(plan.Chunks, plan.Resources.Deposits.Length, depositIndex =>
			{
				WorldCompilerDeposit deposit = plan.Resources.Deposits[depositIndex];
				return (deposit.X - deposit.RadiusX,
					deposit.X + deposit.RadiusX + 1);
			}), Empty, Empty);

	internal static WorldCompilerRuntimeWorkIndex BuildStructures(
		WorldCompilerPlan plan) => new(Empty, Empty, Empty, Empty,
			Index(plan.Chunks, plan.Structures.Corridors.Length, corridorIndex =>
			{
				WorldCompilerCorridor corridor =
					plan.Structures.Corridors[corridorIndex];
				WorldCompilerRoom from = plan.Structures.Rooms[corridor.From];
				WorldCompilerRoom to = plan.Structures.Rooms[corridor.To];
				return (Math.Min(from.CenterX, to.CenterX) - corridor.Width,
					Math.Max(from.CenterX, to.CenterX) + corridor.Width + 1);
			}),
			Index(plan.Chunks, plan.Structures.Rooms.Length, roomIndex =>
			{
				WorldCompilerRect bounds = plan.Structures.Rooms[roomIndex].Bounds;
				return (bounds.X, bounds.Right);
			}));

	private static int[][] Empty => Array.Empty<int[]>();

	private static int[][] Index(WorldCompilerChunk[] chunks, int count,
		Func<int, (int Left, int Right)> bounds)
	{
		var counts = new int[chunks.Length];
		for (int item = 0; item < count; item++)
		{
			(int left, int right) = bounds(item);
			for (int chunkIndex = FirstOverlap(chunks, left);
				chunkIndex < chunks.Length && chunks[chunkIndex].Left < right;
				chunkIndex++) counts[chunkIndex]++;
		}
		var result = new int[chunks.Length][];
		for (int index = 0; index < result.Length; index++)
			result[index] = counts[index] == 0 ? Array.Empty<int>() :
				new int[counts[index]];
		Array.Clear(counts);
		for (int item = 0; item < count; item++)
		{
			(int left, int right) = bounds(item);
			for (int chunkIndex = FirstOverlap(chunks, left);
				chunkIndex < chunks.Length && chunks[chunkIndex].Left < right;
				chunkIndex++)
				result[chunkIndex][counts[chunkIndex]++] = item;
		}
		return result;
	}

	private static int FirstOverlap(WorldCompilerChunk[] chunks, int left)
	{
		int low = 0, high = chunks.Length;
		while (low < high)
		{
			int middle = low + (high - low) / 2;
			if (chunks[middle].Right <= left) low = middle + 1;
			else high = middle;
		}
		return low;
	}

	private static long Count(int[][] entries)
	{
		long count = 0;
		foreach (int[] entry in entries) count += entry.Length;
		return count;
	}
}
