namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static class WorldCompilerCompatibilityPhases
{
	// Terrain providers must precede native objects, chests and walkable corridors.
	// Keep their original relative order; later structure/loot providers still run once.
	internal static bool BeforeStructures(string name) => name is
		"Thorium Mod: Shinies" or "SOTS: Geodes";
}
