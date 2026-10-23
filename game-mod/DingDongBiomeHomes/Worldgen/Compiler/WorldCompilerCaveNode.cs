namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerCaveNode
{
	internal int Id { get; }
	internal int X { get; }
	internal int Y { get; }
	internal int Radius { get; }

	internal WorldCompilerCaveNode(int id, int x, int y, int radius)
	{
		Id = id;
		X = x;
		Y = y;
		Radius = radius;
	}
}
