using System;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes.Fishing.Worldgen;

public enum FishingRegionShape : byte { Ocean, Lake, River, Abyss }
public readonly record struct FishingWaterRegion(int Id, Rectangle Bounds, int WaterY,
	FishingRegionShape Shape, FishingHabitat Habitat, int Seed)
{
	// Generation/replay input, persisted server-side. Runtime clients need only
	// the actual Bounds, so this does not change the world-data wire format.
	public int OceanProfileDepth { get; init; }
	public const int BasinWallThickness = 2;
	public Rectangle PlacementBounds => new(Bounds.X - BasinWallThickness, Bounds.Y - BasinWallThickness,
		Bounds.Width + BasinWallThickness * 2, Bounds.Height + BasinWallThickness * 2);
	// The seal must extend beyond Bounds: lakes touch its edge and rivers have open ends.
	public bool IsBasinWall(int x, int y)
	{
		if (y < WaterY || Contains(x, y)) return false;
		for (int dx = -BasinWallThickness; dx <= BasinWallThickness; dx++)
			for (int dy = -BasinWallThickness; dy <= BasinWallThickness; dy++)
				if (Contains(x + dx, y + dy)) return true;
		return false;
	}

	public bool Contains(int x, int y)
	{
		if (!Bounds.Contains(x, y)) return false;
		if (Shape is FishingRegionShape.Ocean or FishingRegionShape.Abyss) return true;
		if (Shape == FishingRegionShape.Lake)
		{
			double dx = (x - Bounds.Center.X) / (Bounds.Width * 0.5);
			double dy = (y - Bounds.Center.Y) / (Bounds.Height * 0.5);
			return dx * dx + dy * dy <= 1;
		}
		if (Math.Abs(y - FishingWaterGeometry.RiverY(Bounds, x, Seed)) < Bounds.Height / 16) return true;
		for (int basin = 0; basin < 2; basin++)
		{
			double fraction = basin == 0 ? 0.25 : 0.75;
			double dx = (x - (Bounds.Left + Bounds.Width * fraction)) / (Bounds.Width * 0.14);
			double dy = (y - (Bounds.Top + Bounds.Height * 0.6)) / (Bounds.Height * 0.35);
			if (dx * dx + dy * dy <= 1) return true;
		}
		return false;
	}
}

public static class FishingWaterGeometry
{
	public static int OceanWidth(int worldWidth, int nativeWidth) => Math.Clamp(Math.Min(nativeWidth * 3, worldWidth / 6), 180, Math.Max(180, worldWidth / 6));
	public static int OceanDepth(int nativeDepth, int worldHeight) => Math.Clamp((int)Math.Round(nativeDepth * 1.8), 96, Math.Max(96, worldHeight / 4));
	public static int RiverY(Rectangle bounds, int x, int seed) => bounds.Top + bounds.Height / 3 +
		(int)(Math.Sin((x - bounds.Left) * Math.PI * 4 / bounds.Width + seed % 100) * bounds.Height * 0.08);
	public static int OceanFloor(int edgeDistance, int width, int waterY, int depth, int seed)
	{
		double offshore = Math.Clamp(1.0 - (double)edgeDistance / width, 0, 1);
		double shelf = Math.Pow(offshore, 0.65);
		double relief = Math.Sin(edgeDistance * 0.011 + seed % 100) * 0.10 + Math.Sin(edgeDistance * 0.031) * 0.04;
		return waterY + Math.Max(4, (int)(depth * Math.Clamp(shelf + relief * offshore, 0.025, 1.15)));
	}
}
