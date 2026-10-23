using System;

namespace DingDongBiomeHomes.Worldgen;

internal readonly record struct NativeCoastBand(int Width, int Bottom)
{
	internal bool Valid(int worldWidth, int worldHeight) => Width >= 0 && Width <= worldWidth / 4 &&
		Bottom >= 0 && Bottom < worldHeight && (Width == 0) == (Bottom == 0);
	internal int Distance(int native, int nativeBeach) => Width == 0 ? native : Math.Max(20, Width + native - nativeBeach);
	internal bool Contains(int edge, int y) => Width > 0 && edge >= 0 && edge < Width && y >= 0 && y <= Bottom;
}
