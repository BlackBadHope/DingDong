namespace DingDongBiomeHomes.Worldgen.Compiler;

internal sealed class WorldCompilerCavePlan
{
	internal WorldCompilerCaveNode[] Nodes { get; }
	internal WorldCompilerCaveEdge[] Edges { get; }
	internal int Columns { get; }
	internal int Rows { get; }
	internal ulong Fingerprint { get; }

	internal WorldCompilerCavePlan(WorldCompilerCaveNode[] nodes,
		WorldCompilerCaveEdge[] edges, int columns, int rows, ulong fingerprint)
	{
		Nodes = nodes;
		Edges = edges;
		Columns = columns;
		Rows = rows;
		Fingerprint = fingerprint;
	}
}
