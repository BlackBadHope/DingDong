namespace DingDongBiomeHomes.Worldgen.Compiler;

internal enum WorldCompilerPhase
{
	MacroPlan,
	BaseTerrain,
	ClimateStrata,
	CavernGraph,
	Hydrology,
	Structures,
	NaturalDecoration,
	Compatibility,
	Finalize,
	Receipt
}

internal sealed class WorldCompilerPhaseDescriptor
{
	internal WorldCompilerPhase Id { get; }
	internal WorldCompilerPhase[] Dependencies { get; }

	internal WorldCompilerPhaseDescriptor(WorldCompilerPhase id,
		params WorldCompilerPhase[] dependencies)
	{
		Id = id;
		Dependencies = dependencies;
	}
}
