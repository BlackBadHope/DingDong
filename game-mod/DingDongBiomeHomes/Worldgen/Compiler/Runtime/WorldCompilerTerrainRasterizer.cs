using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerTerrainRasterizer
{
	internal static WorldCompilerTerrainReceipt Generate(WorldCompilerPlan plan)
	{
		if (Main.maxTilesX != plan.Width || Main.maxTilesY != plan.Height)
			throw new InvalidOperationException("World compiler dimensions drifted.");
		var clock = Stopwatch.StartNew();
		Main.tile.ClearEverything();
		long solidTiles = 0;
		long waterTiles = 0;
		int workers = WorldCompilerExecutionPolicy.Workers;
		Parallel.For(0, plan.Chunks.Length,
			WorldCompilerExecutionPolicy.ParallelOptions, index =>
			{
				(long solid, long water) = RasterChunk(plan, plan.Chunks[index]);
				Interlocked.Add(ref solidTiles, solid);
				Interlocked.Add(ref waterTiles, water);
			});
		Main.worldSurface = plan.Surface[plan.Width / 2];
		Main.rockLayer = Math.Min(plan.Height - 700,
			Main.worldSurface + plan.Height / 10);
		Main.spawnTileX = plan.Width / 2;
		Main.spawnTileY = plan.Surface[Main.spawnTileX] - 3;
		return new WorldCompilerTerrainReceipt(solidTiles, waterTiles,
			plan.Chunks.Length, workers, clock.ElapsedMilliseconds);
	}

	private static (long Solid, long Water) RasterChunk(WorldCompilerPlan plan,
		WorldCompilerChunk chunk)
	{
		long solid = 0;
		long water = 0;
		int seaLevel = WorldCompilerTerrainSampler.SeaLevel(plan);
		for (int x = chunk.Left; x < chunk.Right; x++)
		{
			WorldCompilerBiome biome = WorldCompilerTerrainSampler.BiomeAt(plan, x);
			int ground = plan.Surface[x];
			if (biome == WorldCompilerBiome.Ocean)
				for (int y = seaLevel; y < ground; y++)
				{
					Tile tile = Main.tile[x, y];
					tile.LiquidType = LiquidID.Water;
					tile.LiquidAmount = byte.MaxValue;
					water++;
				}
			solid += RasterSolidColumn(plan.Height, x, ground, biome);
		}
		return (solid, water);
	}
}
