using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerPlanBuilder
{
	internal static WorldCompilerPlan Build(int width, int height, ulong seed)
	{
		WorldCompilerBand[] bands = WorldCompilerBandPlanner.Create(width, seed);
		WorldCompilerSurfaceProfile surface =
			WorldCompilerSurfaceProfile.Create(width, height, seed, bands);
		WorldCompilerChunk[] chunks = WorldCompilerChunkPartitioner.Create(width);
		WorldCompilerCavePlan caves = WorldCompilerCavePlanner.Create(width, height,
			seed, surface);
		WorldCompilerStructurePlan structures = WorldCompilerStructurePlanner.Create(
			width, height, seed, bands, surface);
		WorldCompilerHydrologyPlan hydrology = WorldCompilerHydrologyPlanner.Create(
			width, height, seed, bands, surface, structures);
		WorldCompilerResourcePlan resources = WorldCompilerResourcePlanner.Create(
			width, height, seed, surface);
		WorldCompilerDecorationPlan decorations = WorldCompilerDecorationPlanner.Create(
			width, height, seed, bands, surface, caves, structures, hydrology);
		WorldCompilerPhase[] phases = WorldCompilerPhaseGraph.DefaultOrder();
		ulong fingerprint = Fingerprint(width, height, seed, bands, surface, caves,
			structures, hydrology, resources, decorations, phases);
		return new WorldCompilerPlan(width, height, seed, bands, surface, chunks, caves,
			structures, hydrology, resources, decorations, phases, fingerprint);
	}

	private static ulong Fingerprint(int width, int height, ulong seed,
		WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface,
		WorldCompilerCavePlan caves,
		WorldCompilerStructurePlan structures,
		WorldCompilerHydrologyPlan hydrology, WorldCompilerResourcePlan resources,
		WorldCompilerDecorationPlan decorations,
		WorldCompilerPhase[] phases)
	{
		ulong hash = 1469598103934665603UL;
		Add(ref hash, width);
		Add(ref hash, height);
		Add(ref hash, unchecked((long)seed));
		foreach (WorldCompilerBand band in bands)
		{
			Add(ref hash, band.Left);
			Add(ref hash, band.Right);
			Add(ref hash, (int)band.Biome);
		}
		int stride = Math.Max(1, width / 257);
		for (int x = 0; x < width; x += stride) Add(ref hash, surface[x]);
		Add(ref hash, unchecked((long)caves.Fingerprint));
		Add(ref hash, unchecked((long)structures.Fingerprint));
		Add(ref hash, unchecked((long)hydrology.Fingerprint));
		Add(ref hash, unchecked((long)resources.Fingerprint));
		Add(ref hash, unchecked((long)decorations.Fingerprint));
		foreach (WorldCompilerPhase phase in phases) Add(ref hash, (int)phase);
		return hash;
	}

	private static void Add(ref ulong hash, long value)
	{
		unchecked
		{
			hash ^= (ulong)value;
			hash *= 1099511628211UL;
		}
	}
}
