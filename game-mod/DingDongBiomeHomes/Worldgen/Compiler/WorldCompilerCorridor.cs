namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerCorridor
{
	internal int From { get; }
	internal int To { get; }
	internal int Width { get; }

	internal WorldCompilerCorridor(int from, int to, int width)
	{
		From = from;
		To = to;
		Width = width;
	}
}
