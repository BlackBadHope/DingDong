namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerHydrologyPlan
{
	internal WorldCompilerBasin[] Basins { get; }
	internal ulong Fingerprint { get; }

	internal WorldCompilerHydrologyPlan(WorldCompilerBasin[] basins,
		ulong fingerprint)
	{
		Basins = basins;
		Fingerprint = fingerprint;
	}
}
