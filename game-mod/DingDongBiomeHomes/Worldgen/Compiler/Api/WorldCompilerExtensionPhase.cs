namespace DingDongBiomeHomes.Worldgen.Compiler.Api;

internal enum WorldCompilerExtensionPhase
{
	AfterBaseTerrain,
	AfterCavernGraph,
	AfterResources,
	AfterHydrology,
	AfterStructures,
	AfterWorldContracts,
	AfterCompatibility,
	AfterNaturalDecoration,
	BeforeFinalize
}
