namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerPlan
{
	internal int Width { get; }
	internal int Height { get; }
	internal ulong Seed { get; }
	internal WorldCompilerBand[] Bands { get; }
	internal WorldCompilerSurfaceProfile Surface { get; }
	internal WorldCompilerChunk[] Chunks { get; }
	internal WorldCompilerCavePlan Caves { get; }
	internal WorldCompilerStructurePlan Structures { get; }
	internal WorldCompilerHydrologyPlan Hydrology { get; }
	internal WorldCompilerResourcePlan Resources { get; }
	internal WorldCompilerDecorationPlan Decorations { get; }
	internal WorldCompilerPhase[] Phases { get; }
	internal ulong Fingerprint { get; }

	internal WorldCompilerPlan(int width, int height, ulong seed,
		WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface,
		WorldCompilerChunk[] chunks, WorldCompilerCavePlan caves,
		WorldCompilerStructurePlan structures, WorldCompilerHydrologyPlan hydrology,
		WorldCompilerResourcePlan resources,
		WorldCompilerDecorationPlan decorations,
		WorldCompilerPhase[] phases, ulong fingerprint)
	{
		Width = width;
		Height = height;
		Seed = seed;
		Bands = bands;
		Surface = surface;
		Chunks = chunks;
		Caves = caves;
		Structures = structures;
		Hydrology = hydrology;
		Resources = resources;
		Decorations = decorations;
		Phases = phases;
		Fingerprint = fingerprint;
	}
}
