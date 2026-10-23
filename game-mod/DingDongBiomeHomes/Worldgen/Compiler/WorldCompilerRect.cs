using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly struct WorldCompilerRect
{
	internal int X { get; }
	internal int Y { get; }
	internal int Width { get; }
	internal int Height { get; }
	internal int Right => X + Width;
	internal int Bottom => Y + Height;

	internal WorldCompilerRect(int x, int y, int width, int height)
	{
		if (width <= 0 || height <= 0)
			throw new ArgumentOutOfRangeException(nameof(width));
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	internal bool Intersects(WorldCompilerRect other) =>
		X < other.Right && Right > other.X && Y < other.Bottom && Bottom > other.Y;

	internal WorldCompilerRect Inflate(int padding) => new(X - padding,
		Y - padding, checked(Width + padding * 2), checked(Height + padding * 2));
}

internal readonly struct WorldCompilerPoint
{
	internal int X { get; }
	internal int Y { get; }
	internal WorldCompilerPoint(int x, int y) { X = x; Y = y; }
}
