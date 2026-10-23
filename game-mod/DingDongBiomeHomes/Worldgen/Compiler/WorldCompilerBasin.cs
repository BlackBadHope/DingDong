namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerBasin
{
	internal int X { get; }
	internal int Y { get; }
	internal int RadiusX { get; }
	internal int RadiusY { get; }
	internal WorldCompilerFluid Fluid { get; }
	internal WorldCompilerRect Bounds => new(X - RadiusX, Y - RadiusY,
		RadiusX * 2 + 1, RadiusY * 2 + 1);

	internal WorldCompilerBasin(int x, int y, int radiusX, int radiusY,
		WorldCompilerFluid fluid)
	{
		X = x;
		Y = y;
		RadiusX = radiusX;
		RadiusY = radiusY;
		Fluid = fluid;
	}
}
