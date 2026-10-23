using System;

namespace DingDongBiomeHomes.Compatibility;

// All coordinates are UI units, before Main.UIScaleMatrix is applied.
internal static class UncraftButtonPlacement
{
	internal const int Size = 40;
	internal const int Gap = 8;

	internal static (int X, int Y) Resolve(int uiWidth, int uiHeight, int trashOffsetX, int trashOffsetY,
		int configuredX, int configuredY, int autoX, int autoY, int autoWidth, int autoHeight)
	{
		int maxX = Math.Max(0, uiWidth - Size), maxY = Math.Max(0, uiHeight - Size);
		int x = Math.Clamp(353 + trashOffsetX + configuredX, 0, maxX);
		int y = Math.Clamp(258 + trashOffsetY + configuredY, 0, maxY);
		if (!Overlaps(x, y, autoX, autoY, autoWidth, autoHeight)) return (x, y);
		// Preserve custom offsets unless they collide with AutoTrash. Try the same
		// row first; only a narrow viewport can require a different row.
		(int X, int Y)[] alternatives = {
			(Math.Clamp(autoX - Gap - Size, 0, maxX), y),
			(Math.Clamp(autoX + autoWidth + Gap, 0, maxX), y),
			(x, Math.Clamp(autoY - Gap - Size, 0, maxY)),
			(x, Math.Clamp(autoY + autoHeight + Gap, 0, maxY))
		};
		foreach (var point in alternatives)
			if (!Overlaps(point.X, point.Y, autoX, autoY, autoWidth, autoHeight)) return point;
		return (x, y);
	}

	internal static bool Overlaps(int x, int y, int ax, int ay, int aw, int ah) =>
		aw > 0 && ah > 0 && x < ax + aw + Gap && x + Size > ax - Gap &&
		y < ay + ah + Gap && y + Size > ay - Gap;
}
