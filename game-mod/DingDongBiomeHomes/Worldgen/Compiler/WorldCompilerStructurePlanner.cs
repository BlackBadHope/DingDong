using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerStructurePlanner
{
	internal static WorldCompilerStructurePlan Create(int width, int height,
		ulong seed, WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface)
	{
		var rooms = new List<WorldCompilerRoom>();
		var corridors = new List<WorldCompilerCorridor>();
		var occupied = new WorldCompilerSpatialIndex(128);
		AddSpawn(width, surface, rooms, occupied);
		AddPyramids(bands, surface, rooms, occupied);
		AddFloatingIslands(width, height, seed, surface, rooms, occupied);
		AddDungeon(width, height, seed, bands, surface, rooms, corridors, occupied);
		AddTemple(height, bands, surface, rooms, corridors, occupied);
		foreach (WorldCompilerAccess access in WorldCompilerAccessPlan.Create(surface, rooms))
			if (!occupied.TryReserve(access.Shaft, 4))
				throw new InvalidOperationException("Structure entrance overlaps another structure.");
		AddResidenceCaverns(height, bands, rooms, occupied);
		AddProgressionSites(width, height, seed, bands, surface, rooms, occupied);
		AddJungleResidencePlot(bands, surface, rooms, occupied);
		return new WorldCompilerStructurePlan(rooms.ToArray(), corridors.ToArray(),
			Fingerprint(rooms, corridors));
	}

	private static void AddSpawn(int width, WorldCompilerSurfaceProfile surface,
		List<WorldCompilerRoom> rooms, WorldCompilerSpatialIndex occupied)
	{
		int x = width / 2;
		AddRoom(rooms, occupied, WorldCompilerStructureKind.SpawnSanctuary,
			new WorldCompilerRect(x - 110, surface[x] - 54, 220, 70), 16);
	}

	private static void AddPyramids(WorldCompilerBand[] bands,
		WorldCompilerSurfaceProfile surface, List<WorldCompilerRoom> rooms,
		WorldCompilerSpatialIndex occupied)
	{
		WorldCompilerBand desert = FindBand(bands, WorldCompilerBiome.Sahara);
		for (int index = 1; index <= 3; index++)
		{
			int x = desert.Left + desert.Width * index / 4;
			AddRoom(rooms, occupied, WorldCompilerStructureKind.DesertPyramid,
				new WorldCompilerRect(x - 70, surface[x] - 150, 140, 160), 28);
		}
	}

	private static void AddFloatingIslands(int width, int height, ulong seed,
		WorldCompilerSurfaceProfile surface, List<WorldCompilerRoom> rooms,
		WorldCompilerSpatialIndex occupied)
	{
		// Late ocean providers reshape coastal terrain, including low sky. Keep
		// progression islands in the continental interior, outside that envelope.
		int margin = Math.Max(700, width / 10);
		int target = Math.Clamp(width / 1_200, 8, 32);
		for (int index = 0; index < target; index++)
		{
			int x = margin + (width - margin * 2) * (index * 2 + 1) / (target * 2);
			x += WorldCompilerSeed.Range(seed, index, 211, -160, 161);
			int ceiling = Math.Max(220, surface[x] - 360);
			int y = WorldCompilerSeed.Range(seed, index, 223, 180,
				Math.Max(181, Math.Min(ceiling, height / 5)));
			x = FloatingIslandX(width, margin, x, y, occupied);
			AddRoom(rooms, occupied, WorldCompilerStructureKind.FloatingIsland,
				new WorldCompilerRect(x - 70, y - 28, 140, 56), 24);
		}
	}

	private static void AddRoom(List<WorldCompilerRoom> rooms,
		WorldCompilerSpatialIndex occupied, WorldCompilerStructureKind kind,
		WorldCompilerRect bounds, int padding)
	{
		if (!occupied.TryReserve(bounds, padding))
			throw new InvalidOperationException($"Structure plan overlap: {kind}.");
		rooms.Add(new WorldCompilerRoom(rooms.Count, kind, bounds));
	}

	private static WorldCompilerBand FindBand(WorldCompilerBand[] bands,
		WorldCompilerBiome biome)
	{
		foreach (WorldCompilerBand band in bands)
			if (band.Biome == biome) return band;
		throw new InvalidOperationException($"Missing macro biome: {biome}.");
	}
}
