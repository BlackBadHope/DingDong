using Terraria.ModLoader;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

public sealed class Exact66WorldgenAdapterSystem : ModSystem
{
	public override void Load() => Exact66WorldgenAdapters.Install(Mod);

	public override void PreWorldGen()
	{
		Exact66WorldgenAdapters.PrepareWorldgen();
	}

	public override void Unload() => Exact66WorldgenAdapters.Uninstall();
}
