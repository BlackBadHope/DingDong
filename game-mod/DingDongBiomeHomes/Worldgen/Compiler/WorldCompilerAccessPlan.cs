using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal readonly record struct WorldCompilerAccess(int RoomId, int X, int Top,
	int Bottom, int TargetX, bool Locked)
{
	internal WorldCompilerRect Shaft => new(X - 5, Top, 11, Bottom - Top + 6);
	internal WorldCompilerRect Hall => new(X - 5, Bottom - 3,
		TargetX - X + 11, 7);
}

internal static class WorldCompilerAccessPlan
{
	internal static WorldCompilerAccess[] Create(WorldCompilerSurfaceProfile surface,
		IReadOnlyList<WorldCompilerRoom> rooms)
	{
		var result = new List<WorldCompilerAccess>();
		bool dungeon = false, temple = false;
		foreach (WorldCompilerRoom room in rooms)
		{
			bool locked = room.Kind == WorldCompilerStructureKind.TempleRoom;
			if (locked ? temple : room.Kind != WorldCompilerStructureKind.DungeonRoom || dungeon)
				continue;
			int x = room.Bounds.X - 22;
			result.Add(new(room.Id, x, surface[x] - 6, room.CenterY,
				room.CenterX, locked));
			if (locked) temple = true; else dungeon = true;
		}
		return result.ToArray();
	}
}
