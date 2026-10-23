using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen;

internal sealed class AuthoredCacheLifetimeSystem : ModSystem
{
	// Catch consumers outside the residence pass (audits, commands, load gates).
	public override void PostWorldGen() => DingDongAuthoredStructureLoader.ReleaseIdleCaches(Mod, "post-worldgen");
	public override void OnWorldUnload() => DingDongAuthoredStructureLoader.ReleaseIdleCaches(Mod, "world-unload");
	public override void Unload() => DingDongAuthoredStructureLoader.ReleaseIdleCaches(Mod, "mod-unload");
}
