using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerResourceRasterizer
{
	internal static WorldCompilerResourceReceipt Generate(WorldCompilerPlan plan)
	{
		var clock = Stopwatch.StartNew();
		long ore = 0;
		long infected = 0;
		long infectionCandidates = 0;
		Parallel.For(0, plan.Chunks.Length,
			WorldCompilerExecutionPolicy.ParallelOptions, index =>
			{
				(long localOre, long localInfected, long localCandidates) = RasterChunk(plan,
					plan.Chunks[index]);
				Interlocked.Add(ref ore, localOre);
				Interlocked.Add(ref infected, localInfected);
				Interlocked.Add(ref infectionCandidates, localCandidates);
			});
		return new WorldCompilerResourceReceipt(ore, infected, infectionCandidates,
			plan.Resources.Deposits.Length, clock.ElapsedMilliseconds);
	}

	private static (long Ore, long Infected, long Candidates) RasterChunk(
		WorldCompilerPlan plan,
		WorldCompilerChunk chunk)
	{
		(long infected, long candidates) = RasterInfections(plan, chunk);
		long ore = 0;
		WorldCompilerRuntimeWorkIndex work = WorldCompilerRuntimeState.WorkIndex ??
			throw new InvalidOperationException("World compiler work index is unavailable.");
		foreach (int depositIndex in work.Deposits[chunk.Index])
		{
			WorldCompilerDeposit deposit = plan.Resources.Deposits[depositIndex];
			ore += RasterDeposit(plan.Height, chunk, deposit);
		}
		return (ore, infected, candidates);
	}

	private static (long Changed, long Candidates) RasterInfections(
		WorldCompilerPlan plan,
		WorldCompilerChunk chunk)
	{
		long changed = 0;
		long candidates = 0;
		int cutoff = plan.Height * 84 / 100;
		foreach (WorldCompilerInfection infection in plan.Resources.Infections)
		{
			(int boundLeft, int boundRight) =
				WorldCompilerInfectionWindow.HorizontalBounds(plan.Width,
					plan.Height, infection);
			int left = Math.Max(chunk.Left, boundLeft);
			int right = Math.Min(chunk.Right, boundRight);
			for (int x = left; x < right; x++)
		{
			int ground = plan.Surface[x];
			if (!WorldCompilerInfectionWindow.TryDepthRange(infection, x,
				cutoff - ground, out int start, out int end)) continue;
			for (int depth = start; depth < end; depth++)
			{
				int y = ground + depth;
				candidates++;
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile) continue;
				ushort converted = Convert(tile.TileType, infection.Kind);
				if (converted == tile.TileType) continue;
				tile.TileType = converted;
				changed++;
			}
		}
		}
		return (changed, candidates);
	}

	private static long RasterDeposit(int height, WorldCompilerChunk chunk,
		WorldCompilerDeposit deposit)
	{
		int left = Math.Max(chunk.Left, deposit.X - deposit.RadiusX);
		int right = Math.Min(chunk.Right - 1, deposit.X + deposit.RadiusX);
		int[] profile = WorldCompilerEllipseProfile.Get(deposit.RadiusX,
			deposit.RadiusY);
		long changed = 0;
		for (int x = left; x <= right; x++)
		{
			int extent = profile[x - deposit.X + deposit.RadiusX];
			int top = Math.Max(1, deposit.Y - extent);
			int bottom = Math.Min(height - 2, deposit.Y + extent);
			for (int y = top; y <= bottom; y++)
			{
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile) continue;
			tile.TileType = OreType(deposit.Kind);
			changed++;
			}
		}
		return changed;
	}

	private static ushort OreType(WorldCompilerDepositKind kind) => kind switch
	{
		WorldCompilerDepositKind.Iron => TileID.Iron,
		WorldCompilerDepositKind.Silver => TileID.Silver,
		WorldCompilerDepositKind.Gold => TileID.Gold,
		WorldCompilerDepositKind.Demonite => TileID.Demonite,
		WorldCompilerDepositKind.Crimtane => TileID.Crimtane,
		WorldCompilerDepositKind.Hellstone => TileID.Hellstone,
		_ => TileID.Copper
	};

	private static ushort Convert(ushort tile,
		WorldCompilerInfectionKind kind) => (kind, tile) switch
	{
		(WorldCompilerInfectionKind.Corruption, TileID.Stone) => TileID.Ebonstone,
		(WorldCompilerInfectionKind.Corruption, TileID.Sand) => TileID.Ebonsand,
		(WorldCompilerInfectionKind.Corruption, TileID.Grass) => TileID.CorruptGrass,
		(WorldCompilerInfectionKind.Crimson, TileID.Stone) => TileID.Crimstone,
		(WorldCompilerInfectionKind.Crimson, TileID.Sand) => TileID.Crimsand,
		(WorldCompilerInfectionKind.Crimson, TileID.Grass) => TileID.CrimsonGrass,
		(WorldCompilerInfectionKind.Hallow, TileID.Stone) => TileID.Pearlstone,
		(WorldCompilerInfectionKind.Hallow, TileID.Sand) => TileID.Pearlsand,
		(WorldCompilerInfectionKind.Hallow, TileID.Grass) => TileID.HallowedGrass,
		_ => tile
	};
}
