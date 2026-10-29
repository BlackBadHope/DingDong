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
		// V03 28.10: plain ellipses read as dirt-capped "eggs" on the owner's map
		// (17.webp). Lakes and river pools now have a seeded lobed rim, rivers a
		// channel of varying bore; still a pure function of Bounds and Seed.
		if (Shape == FishingRegionShape.Lake)
			return InLobe(x, y, Bounds.Center.X, Bounds.Center.Y, Bounds.Width * 0.5, Bounds.Height * 0.5, 0);
		double bore = Bounds.Height / 16.0 * (0.8 + 0.35 * Math.Sin((x - Bounds.Left) * 0.043 + Phase(3)));
		if (Math.Abs(y - FishingWaterGeometry.RiverY(Bounds, x, Seed)) < bore) return true;
		for (int basin = 0; basin < 2; basin++)
		{
			double fraction = (basin == 0 ? 0.25 : 0.75) + 0.05 * Math.Sin(Phase(4 + basin));
			if (InLobe(x, y, Bounds.Left + Bounds.Width * fraction, Bounds.Top + Bounds.Height * 0.55,
				Bounds.Width * 0.17, Bounds.Height * 0.26, 1 + basin)) return true;
		}
		return false;
	}
	// V03 28.10: 0..2*pi phase derived from Seed (non-negative from genRand.Next()).
	private double Phase(int k) => (uint)Seed * 2654435761u * (uint)(k + 1) % 6283u / 1000.0;
	// Radius limit 0.62..1.0 of the half-axes; three harmonics give lobes and an off-centre deep end.
	private bool InLobe(int x, int y, double cx, double cy, double rx, double ry, int salt)
	{
		double dx = (x - cx) / rx, dy = (y - cy) / ry, r2 = dx * dx + dy * dy;
		if (r2 > 1) return false;
		if (r2 < 0.38) return true;
		double a = Math.Atan2(dy, dx);
		double rim = 0.81 + 0.10 * Math.Sin(3 * a + Phase(salt * 3)) + 0.06 * Math.Sin(5 * a + Phase(salt * 3 + 1))
			+ 0.03 * Math.Sin(9 * a + Phase(salt * 3 + 2));
		return r2 <= rim * rim;
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
