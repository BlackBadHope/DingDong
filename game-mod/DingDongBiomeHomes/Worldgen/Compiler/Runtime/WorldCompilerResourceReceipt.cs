namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly struct WorldCompilerResourceReceipt
{
	internal long OreTiles { get; }
	internal long InfectedTiles { get; }
	internal long InfectionCandidates { get; }
	internal int Deposits { get; }
	internal long ElapsedMilliseconds { get; }

	internal WorldCompilerResourceReceipt(long oreTiles, long infectedTiles,
		long infectionCandidates, int deposits, long elapsedMilliseconds)
	{
		OreTiles = oreTiles;
		InfectedTiles = infectedTiles;
		InfectionCandidates = infectionCandidates;
		Deposits = deposits;
		ElapsedMilliseconds = elapsedMilliseconds;
	}
}
