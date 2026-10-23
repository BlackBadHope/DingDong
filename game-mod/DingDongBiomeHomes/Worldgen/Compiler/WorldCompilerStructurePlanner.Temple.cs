using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerStructurePlanner
{
	private static void AddTemple(int height, WorldCompilerBand[] bands,
		WorldCompilerSurfaceProfile surface, List<WorldCompilerRoom> rooms,
		List<WorldCompilerCorridor> corridors, WorldCompilerSpatialIndex occupied)
	{
		const int columns = 10;
		const int rows = 8;
		const int roomWidth = 44;
		const int roomHeight = 30;
		const int gapX = 16;
		const int gapY = 14;
		WorldCompilerBand amazon = FindBand(bands, WorldCompilerBiome.Amazon);
		int centerX = amazon.Left + amazon.Width / 2;
		int totalWidth = columns * roomWidth + (columns - 1) * gapX;
		int left = centerX - totalWidth / 2;
		int top = Math.Max(height * 48 / 100, surface[centerX] + height / 12);
		int firstId = rooms.Count;
		for (int row = 0; row < rows; row++)
		for (int column = 0; column < columns; column++)
		{
			int x = left + column * (roomWidth + gapX);
			int y = top + row * (roomHeight + gapY);
			int id = rooms.Count;
			AddRoom(rooms, occupied, WorldCompilerStructureKind.TempleRoom,
				new WorldCompilerRect(x, y, roomWidth, roomHeight), 4);
			if (column > 0)
				corridors.Add(new WorldCompilerCorridor(id - 1, id, 5));
			if (column == 0 && row > 0)
				corridors.Add(new WorldCompilerCorridor(firstId + (row - 1) * columns,
					id, 5));
		}
	}

	private static ulong Fingerprint(List<WorldCompilerRoom> rooms,
		List<WorldCompilerCorridor> corridors)
	{
		ulong hash = 0xCBF29CE484222325UL;
		foreach (WorldCompilerRoom room in rooms)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)room.Bounds.X << 32 ^
				(uint)room.Bounds.Y ^ (uint)room.Kind);
		foreach (WorldCompilerCorridor corridor in corridors)
			hash = WorldCompilerSeed.Mix(hash ^ (ulong)(uint)corridor.From << 32 ^
				(uint)corridor.To ^ (uint)corridor.Width);
		return hash;
	}
}
