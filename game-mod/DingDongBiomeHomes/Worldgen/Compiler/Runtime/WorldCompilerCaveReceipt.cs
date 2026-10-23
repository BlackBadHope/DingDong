namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal readonly struct WorldCompilerCaveReceipt
{
	internal long CarvedTiles { get; }
	internal int Nodes { get; }
	internal int Edges { get; }
	internal long ElapsedMilliseconds { get; }

	internal WorldCompilerCaveReceipt(long carvedTiles, int nodes, int edges,
		long elapsedMilliseconds)
	{
		CarvedTiles = carvedTiles;
		Nodes = nodes;
		Edges = edges;
		ElapsedMilliseconds = elapsedMilliseconds;
	}
}
