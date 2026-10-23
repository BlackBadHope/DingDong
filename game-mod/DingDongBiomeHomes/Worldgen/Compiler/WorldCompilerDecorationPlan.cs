namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerDecorationPlan
{
	internal WorldCompilerDecoration[] Entries { get; }
	internal ulong Fingerprint { get; }

	internal WorldCompilerDecorationPlan(WorldCompilerDecoration[] entries,
		ulong fingerprint)
	{
		Entries = entries;
		Fingerprint = fingerprint;
	}
}
