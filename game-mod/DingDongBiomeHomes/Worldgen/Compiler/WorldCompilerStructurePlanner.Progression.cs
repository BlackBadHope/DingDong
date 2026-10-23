using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerStructurePlanner
{
	private static void AddProgressionSites(int width, int height, ulong seed,
		WorldCompilerBand[] bands, WorldCompilerSurfaceProfile surface,
		List<WorldCompilerRoom> rooms, WorldCompilerSpatialIndex occupied)
	{
		WorldCompilerBand jungle = FindBand(bands, WorldCompilerBiome.Amazon);
		int evilX = width * WorldCompilerSeed.Range(seed, 1, 517, 18, 36) / 100;
		AddSites(WorldCompilerStructureKind.HiveChamber, 6, jungle.Left + 150,
			jungle.Right - 150, height * 34 / 100, height * 66 / 100, 38, 30);
		AddSites(WorldCompilerStructureKind.JungleShrine, 8, jungle.Left + 150,
			jungle.Right - 150, height * 27 / 100, height * 60 / 100, 28, 22);
		AddSites(WorldCompilerStructureKind.EvilShrine, 8, evilX - width / 38,
			evilX + width / 38, surface[evilX] + 100,
			Math.Min(height * 55 / 100, surface[evilX] + 950), 30, 24);
		AddSites(WorldCompilerStructureKind.UnderworldForge, 12, 300,
			width - 300, height - 340, height - 130, 36, 26);

		void AddSites(WorldCompilerStructureKind kind, int count, int left,
			int right, int top, int bottom, int roomWidth, int roomHeight)
		{
			int placed = 0;
			// A finite deterministic lattice; no unbounded placement retries.
			for (int row = 0; row < 12 && placed < count; row++)
			for (int column = 0; column < count * 3 && placed < count; column++)
			{
				int x = left + (right - left) * (column * 2 + 1) / (count * 6);
				int y = top + (bottom - top) * row / 11;
				var bounds = new WorldCompilerRect(x - roomWidth / 2, y,
					roomWidth, roomHeight);
				if (!occupied.TryReserve(bounds, 24)) continue;
				rooms.Add(new WorldCompilerRoom(rooms.Count, kind, bounds));
				placed++;
			}
			if (placed != count)
				throw new InvalidOperationException($"Progression site shortage: {kind} {placed}/{count}.");
		}
	}
}
