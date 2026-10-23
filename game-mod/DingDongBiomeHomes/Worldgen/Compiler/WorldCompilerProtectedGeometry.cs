using System;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static class WorldCompilerProtectedGeometry
{
	internal static WorldCompilerSpatialIndex Create(WorldCompilerSurfaceProfile surface,
		WorldCompilerStructurePlan structures, int padding = 3)
	{
		if (padding < 0) throw new ArgumentOutOfRangeException(nameof(padding));
		var index = new WorldCompilerSpatialIndex();
		foreach (WorldCompilerRoom room in structures.Rooms)
		{
			index.ReserveExisting(room.Bounds.Inflate(padding));
		}
		foreach (WorldCompilerAccess access in WorldCompilerAccessPlan.Create(surface, structures.Rooms))
		{
			index.ReserveExisting(access.Shaft.Inflate(padding));
			index.ReserveExisting(access.Hall.Inflate(padding));
			// Includes both the opening and the Old Man's safe outer landing.
			index.ReserveExisting(new WorldCompilerRect(access.X - 12,
				access.Top - 1, 30, 10).Inflate(padding));
		}
		foreach (WorldCompilerCorridor corridor in structures.Corridors)
		{
			WorldCompilerRoom from = structures.Rooms[corridor.From], to = structures.Rooms[corridor.To];
			int radius = Math.Max(2, corridor.Width / 2) + padding;
			index.ReserveExisting(new WorldCompilerRect(Math.Min(from.CenterX, to.CenterX),
				from.CenterY, Math.Abs(to.CenterX - from.CenterX) + 1, 1).Inflate(radius));
			index.ReserveExisting(new WorldCompilerRect(to.CenterX,
				Math.Min(from.CenterY, to.CenterY), 1, Math.Abs(to.CenterY - from.CenterY) + 1).Inflate(radius));
		}
		return index;
	}
}
