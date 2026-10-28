using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class AuthoredCacheLifetimeSystem : ModSystem
{
	// RamOpt 28.10: pick the memory mode of our passes once at world creation.
	public override void PreWorldGen() => RamOptMemoryMode.BeginGeneration(Mod);

	// Catch consumers outside the residence pass (audits, commands, load gates).
	public override void PostWorldGen()
	{
		DingDongAuthoredStructureLoader.ReleaseIdleCaches(Mod, "post-worldgen");
		RamOptMemoryMode.EndGeneration();
		// RamOpt 28.10: hand the x10 generation garbage back to Windows before the final
		// save. Continental worlds only: elsewhere the heap is small and a blocking
		// compaction is not worth the pause.
		if (ContinentalWorldDimensions.IsContinental)
			ContinentalWorldCapacity.ReleaseTransientGarbage("post-worldgen");
	}

	public override void OnWorldUnload() => DingDongAuthoredStructureLoader.ReleaseIdleCaches(Mod, "world-unload");

	public override void Unload()
	{
		DingDongAuthoredStructureLoader.ReleaseIdleCaches(Mod, "mod-unload");
		RamOptMemoryMode.EndGeneration(); // RamOpt 28.10
		RamOptTilePlanes.Reset(); // RamOpt 28.10
	}
}
