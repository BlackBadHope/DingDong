using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerResourcePlanner
{
	internal static WorldCompilerResourcePlan Create(int width, int height,
		ulong seed, WorldCompilerSurfaceProfile surface)
	{
		var deposits = new List<WorldCompilerDeposit>();
		WorldCompilerPoint[] points = WorldCompilerCandidateLattice.Create(
			new WorldCompilerRect(120, height / 4, width - 240, height * 7 / 10),
			210, 170, seed ^ 0x5245534F55524345UL);
		for (int index = 0; index < points.Length; index++)
		{
			WorldCompilerPoint point = points[index];
			if (point.Y <= surface[point.X] + 45 ||
				WorldCompilerSeed.At(seed, index, 503) % 4UL == 0) continue;
			WorldCompilerDepositKind kind = Kind(seed, index, point.Y, height);
			int rx = WorldCompilerSeed.Range(seed, index, 509, 3, 9);
			int ry = WorldCompilerSeed.Range(seed, index, 511, 2, 6);
			deposits.Add(new WorldCompilerDeposit(point.X, point.Y, rx, ry, kind));
		}
		WorldCompilerInfection[] infections = CreateInfections(width, seed);
		return new WorldCompilerResourcePlan(deposits.ToArray(), infections,
			Fingerprint(deposits, infections));
	}

	private static WorldCompilerDepositKind Kind(ulong seed, int index, int y,
		int height)
	{
		if (y > height * 83 / 100) return WorldCompilerDepositKind.Hellstone;
		ulong roll = WorldCompilerSeed.At(seed, index, 507) % 100UL;
		if (roll < 38) return WorldCompilerDepositKind.Copper;
		if (roll < 66) return WorldCompilerDepositKind.Iron;
		if (roll < 83) return WorldCompilerDepositKind.Silver;
		if (roll < 94) return WorldCompilerDepositKind.Gold;
		bool crimson = (WorldCompilerSeed.At(seed, 0, 513) & 1UL) != 0;
		return crimson ? WorldCompilerDepositKind.Crimtane :
			WorldCompilerDepositKind.Demonite;
	}

	private static WorldCompilerInfection[] CreateInfections(int width, ulong seed)
	{
		bool crimson = (WorldCompilerSeed.At(seed, 0, 513) & 1UL) != 0;
		int evilX = width * WorldCompilerSeed.Range(seed, 1, 517, 18, 36) / 100;
		int hallowX = width * WorldCompilerSeed.Range(seed, 2, 519, 64, 82) / 100;
		int halfWidth = Math.Max(420, width / 32);
		return
		[
			new(crimson ? WorldCompilerInfectionKind.Crimson :
				WorldCompilerInfectionKind.Corruption, evilX, halfWidth,
				WorldCompilerSeed.Range(seed, 1, 521, -160, 161)),
			new(WorldCompilerInfectionKind.Hallow, hallowX, halfWidth,
				WorldCompilerSeed.Range(seed, 2, 523, -160, 161))
		];
	}

	private static ulong Fingerprint(List<WorldCompilerDeposit> deposits,
		WorldCompilerInfection[] infections)
	{
		ulong hash = 0x5245534F55524345UL;
		foreach (WorldCompilerDeposit deposit in deposits)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)deposit.X << 32 ^
				(uint)deposit.Y ^ (ulong)deposit.Kind);
		foreach (WorldCompilerInfection infection in infections)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)infection.SurfaceX << 32 ^
				(uint)infection.HalfWidth ^ (ulong)infection.Kind);
		return hash;
	}
}
