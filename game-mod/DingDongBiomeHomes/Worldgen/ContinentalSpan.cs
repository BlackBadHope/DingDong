using System;

namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct ContinentalSpan(int Left, int Right)
{
	internal bool IsValid => Left >= 0 && Right >= Left;
	internal int Width => IsValid ? Right - Left + 1 : 0;
	internal int Center => IsValid ? Left + Width / 2 : -1;
	internal bool Contains(int x) => IsValid && x >= Left && x <= Right;
	internal int DistanceTo(int x) => Contains(x) ? 0 : Math.Min(Math.Abs(x - Left), Math.Abs(x - Right));
	public override string ToString() => IsValid ? $"{Left}:{Right}:{Width}" : "missing";
}
