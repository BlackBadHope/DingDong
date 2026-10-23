namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerDeposit
{
	internal int X { get; }
	internal int Y { get; }
	internal int RadiusX { get; }
	internal int RadiusY { get; }
	internal WorldCompilerDepositKind Kind { get; }

	internal WorldCompilerDeposit(int x, int y, int radiusX, int radiusY,
		WorldCompilerDepositKind kind)
	{
		X = x;
		Y = y;
		RadiusX = radiusX;
		RadiusY = radiusY;
		Kind = kind;
	}
}
