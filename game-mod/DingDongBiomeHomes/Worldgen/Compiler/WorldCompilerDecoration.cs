namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerDecoration
{
	internal WorldCompilerDecorationKind Kind { get; }
	internal int X { get; }
	internal int Y { get; }
	internal int Width { get; }
	internal int Height { get; }
	internal WorldCompilerRect Bounds => new(X - Width / 2, Y - Height,
		Width, Height + 3);

	internal WorldCompilerDecoration(WorldCompilerDecorationKind kind, int x,
		int y, int width, int height)
	{
		Kind = kind;
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}
}
