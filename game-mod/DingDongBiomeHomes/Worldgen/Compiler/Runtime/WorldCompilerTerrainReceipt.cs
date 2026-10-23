namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly struct WorldCompilerTerrainReceipt
{
	internal long SolidTiles { get; }
	internal long WaterTiles { get; }
	internal int Chunks { get; }
	internal int WorkerLimit { get; }
	internal long ElapsedMilliseconds { get; }

	internal WorldCompilerTerrainReceipt(long solidTiles, long waterTiles,
		int chunks, int workerLimit, long elapsedMilliseconds)
	{
		SolidTiles = solidTiles;
		WaterTiles = waterTiles;
		Chunks = chunks;
		WorkerLimit = workerLimit;
		ElapsedMilliseconds = elapsedMilliseconds;
	}
}
