using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerHydrologyRasterizer
{
	internal static WorldCompilerHydrologyReceipt Generate(WorldCompilerPlan plan)
	{
		var clock = Stopwatch.StartNew();
		long carved = 0;
		long liquid = 0;
		Parallel.For(0, plan.Chunks.Length,
			WorldCompilerExecutionPolicy.ParallelOptions, index =>
			{
				(long localCarved, long localLiquid) = RasterChunk(plan,
					plan.Chunks[index]);
				Interlocked.Add(ref carved, localCarved);
				Interlocked.Add(ref liquid, localLiquid);
			});
		return new WorldCompilerHydrologyReceipt(carved, liquid,
			plan.Hydrology.Basins.Length, clock.ElapsedMilliseconds);
	}

	private static (long Carved, long Liquid) RasterChunk(WorldCompilerPlan plan,
		WorldCompilerChunk chunk)
	{
		long carved = 0;
		long liquid = 0;
		WorldCompilerRuntimeWorkIndex work = WorldCompilerRuntimeState.WorkIndex ??
			throw new InvalidOperationException("World compiler work index is unavailable.");
		foreach (int basinIndex in work.Basins[chunk.Index])
		{
			WorldCompilerBasin basin = plan.Hydrology.Basins[basinIndex];
			(long cut, long filled) = RasterBasin(plan.Height, chunk, basin);
			carved += cut;
			liquid += filled;
		}
		return (carved, liquid);
	}

	private static (long Carved, long Liquid) RasterBasin(int height,
		WorldCompilerChunk chunk, WorldCompilerBasin basin)
	{
		int left = Math.Max(chunk.Left, basin.X - basin.RadiusX);
		int right = Math.Min(chunk.Right - 1, basin.X + basin.RadiusX);
		int[] profile = WorldCompilerEllipseProfile.Get(basin.RadiusX,
			basin.RadiusY);
		long carved = 0;
		long liquid = 0;
		for (int x = left; x <= right; x++)
		{
			int extent = profile[x - basin.X + basin.RadiusX];
			int top = Math.Max(1, basin.Y - extent);
			int bottom = Math.Min(height - 2, basin.Y + extent);
			for (int y = top; y <= bottom; y++)
			{
			Tile tile = Main.tile[x, y];
			if (tile.HasTile) carved++;
			tile.ClearEverything();
			if (y < basin.Y) continue;
			tile.LiquidType = LiquidType(basin.Fluid);
			tile.LiquidAmount = byte.MaxValue;
			liquid++;
			}
		}
		return (carved, liquid);
	}

	private static int LiquidType(WorldCompilerFluid fluid) => fluid switch
	{
		WorldCompilerFluid.Honey => LiquidID.Honey,
		WorldCompilerFluid.Lava => LiquidID.Lava,
		_ => LiquidID.Water
	};
}
