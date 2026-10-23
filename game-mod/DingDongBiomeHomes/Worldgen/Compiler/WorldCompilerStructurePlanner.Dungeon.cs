using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerStructurePlanner
{
	private static void AddDungeon(int width, int height, ulong seed,
		WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface,
		List<WorldCompilerRoom> rooms, List<WorldCompilerCorridor> corridors,
		WorldCompilerSpatialIndex occupied)
	{
		WorldCompilerBand arctic = FindBand(bands, WorldCompilerBiome.Arctic);
		int target = Math.Clamp(width / 175, 96, 192);
		int spineCount = target / 2;
		int anchorX = arctic.Left + arctic.Width / 2;
		int top = Math.Max(surface[anchorX] + Math.Max(420, height / 16),
			height * 30 / 100);
		int bottom = height - Math.Max(780, height / 9);
		int previous = -1;
		for (int index = 0; index < spineCount; index++)
		{
			int y = top + (bottom - top) * index / Math.Max(1, spineCount - 1);
			int drift = WorldCompilerSeed.Range(seed, index, 307, -420, 421);
			int x = Math.Clamp(anchorX + drift, arctic.Left + 300,
				arctic.Right - 300);
			int roomWidth = WorldCompilerSeed.Range(seed, index, 311, 42, 69);
			int roomHeight = WorldCompilerSeed.Range(seed, index, 313, 24, 37);
			int main = rooms.Count;
			AddRoom(rooms, occupied, WorldCompilerStructureKind.DungeonRoom,
				new WorldCompilerRect(x - roomWidth / 2, y - roomHeight / 2,
					roomWidth, roomHeight), 4);
			if (previous >= 0)
				corridors.Add(new WorldCompilerCorridor(previous, main, 7));
			previous = main;
			int side = (index & 1) == 0 ? -1 : 1;
			int branchX = x + side * WorldCompilerSeed.Range(seed, index, 317,
				100, 181);
			int branch = rooms.Count;
			AddRoom(rooms, occupied, WorldCompilerStructureKind.DungeonRoom,
				new WorldCompilerRect(branchX - 24, y - 13, 48, 26), 4);
			corridors.Add(new WorldCompilerCorridor(main, branch, 6));
		}
	}
}
