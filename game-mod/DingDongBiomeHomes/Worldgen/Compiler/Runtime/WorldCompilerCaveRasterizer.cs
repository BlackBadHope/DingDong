using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerCaveRasterizer
{
	internal static WorldCompilerCaveReceipt Generate(WorldCompilerPlan plan)
	{
		var clock = Stopwatch.StartNew();
		long carved = 0;
		Parallel.For(0, plan.Chunks.Length,
			WorldCompilerExecutionPolicy.ParallelOptions, index =>
			{
				long local = CarveChunk(plan, plan.Chunks[index]);
				Interlocked.Add(ref carved, local);
			});
		return new WorldCompilerCaveReceipt(carved, plan.Caves.Nodes.Length,
			plan.Caves.Edges.Length, clock.ElapsedMilliseconds);
	}

	private static long CarveChunk(WorldCompilerPlan plan, WorldCompilerChunk chunk)
	{
		WorldCompilerRuntimeWorkIndex work = WorldCompilerRuntimeState.WorkIndex ??
			throw new InvalidOperationException("World compiler work index is unavailable.");
		using var mask = new WorldCompilerChunkMask(chunk, plan.Height);
		foreach (int edgeIndex in work.CaveEdges[chunk.Index])
		{
			WorldCompilerCaveEdge edge = plan.Caves.Edges[edgeIndex];
			WorldCompilerCaveNode from = plan.Caves.Nodes[edge.From];
			WorldCompilerCaveNode to = plan.Caves.Nodes[edge.To];
			MarkEdge(mask, from, to, edge.Radius);
		}
		foreach (int nodeIndex in work.CaveNodes[chunk.Index])
		{
			WorldCompilerCaveNode node = plan.Caves.Nodes[nodeIndex];
			mask.MarkEllipse(node.X, node.Y, node.Radius * 2, node.Radius);
		}
		var visitor = new CaveVisitor();
		mask.Apply(ref visitor);
		return visitor.Carved;
	}

	private static void MarkEdge(WorldCompilerChunkMask mask,
		WorldCompilerCaveNode from, WorldCompilerCaveNode to, int radius)
	{
		int distance = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
		int steps = Math.Max(1, distance / Math.Max(3, radius / 2));
		for (int step = 0; step <= steps; step++)
		{
			int x = from.X + (to.X - from.X) * step / steps;
			int y = from.Y + (to.Y - from.Y) * step / steps;
			mask.MarkEllipse(x, y, radius, radius);
		}
	}

	private struct CaveVisitor : IWorldCompilerChunkMaskVisitor
	{
		internal long Carved;

		public void Visit(int x, int y)
		{
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile) return;
			tile.ClearEverything();
			tile.WallType = WallID.Stone;
			Carved++;
		}
	}
}
