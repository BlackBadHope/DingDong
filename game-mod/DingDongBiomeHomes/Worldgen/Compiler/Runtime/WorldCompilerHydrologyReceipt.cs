namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly struct WorldCompilerHydrologyReceipt
{
	internal long CarvedTiles { get; }
	internal long LiquidTiles { get; }
	internal int Basins { get; }
	internal long ElapsedMilliseconds { get; }

	internal WorldCompilerHydrologyReceipt(long carvedTiles, long liquidTiles,
		int basins, long elapsedMilliseconds)
	{
		CarvedTiles = carvedTiles;
		LiquidTiles = liquidTiles;
		Basins = basins;
		ElapsedMilliseconds = elapsedMilliseconds;
	}
}
