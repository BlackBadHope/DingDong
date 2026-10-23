using System;

namespace DingDongBiomeHomes.Worldgen;

// Matches WorldGenRange.ScaleValue: density and local geometry are different axes.
internal readonly record struct NativeWorldScale
{
	internal int Width { get; }
	internal int Height { get; }
	internal long Area => (long)Width * Height;
	internal double WidthFactor => Width / 4200d;
	internal double AreaFactor => Area / 5040000d;
	internal double HeightFactor => Height / 1200d;

	internal NativeWorldScale(int width, int height)
	{
		if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
		if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
		Width = width;
		Height = height;
	}

	internal int Scale(int value, NativeScaleAxis axis)
	{
		if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
		double factor = axis switch {
			NativeScaleAxis.Local => 1d,
			NativeScaleAxis.Width => WidthFactor,
			NativeScaleAxis.Area => AreaFactor,
			_ => throw new ArgumentOutOfRangeException(nameof(axis))
		};
		return checked((int)(value * factor));
	}
}

internal enum NativeScaleAxis { Local, Width, Area }
