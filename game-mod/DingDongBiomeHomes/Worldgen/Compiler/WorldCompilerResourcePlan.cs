namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerResourcePlan
{
	internal WorldCompilerDeposit[] Deposits { get; }
	internal WorldCompilerInfection[] Infections { get; }
	internal ulong Fingerprint { get; }

	internal WorldCompilerResourcePlan(WorldCompilerDeposit[] deposits,
		WorldCompilerInfection[] infections, ulong fingerprint)
	{
		Deposits = deposits;
		Infections = infections;
		Fingerprint = fingerprint;
	}
}
