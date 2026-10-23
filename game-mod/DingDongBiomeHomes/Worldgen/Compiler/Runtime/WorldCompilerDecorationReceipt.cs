namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly struct WorldCompilerDecorationReceipt
{
	internal long NaturalTiles { get; }
	internal int Chests { get; }
	internal int LifeCrystals { get; }
	internal long ElapsedMilliseconds { get; }

	internal WorldCompilerDecorationReceipt(long naturalTiles, int chests,
		int lifeCrystals, long elapsedMilliseconds)
	{
		NaturalTiles = naturalTiles;
		Chests = chests;
		LifeCrystals = lifeCrystals;
		ElapsedMilliseconds = elapsedMilliseconds;
	}
}
