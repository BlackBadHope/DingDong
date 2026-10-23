namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerCaveEdge
{
	internal int From { get; }
	internal int To { get; }
	internal int Radius { get; }

	internal WorldCompilerCaveEdge(int from, int to, int radius)
	{
		From = from;
		To = to;
		Radius = radius;
	}
}
