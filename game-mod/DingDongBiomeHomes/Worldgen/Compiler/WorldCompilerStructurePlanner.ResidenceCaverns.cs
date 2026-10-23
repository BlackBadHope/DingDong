using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes.Worldgen.Compiler;

internal static partial class WorldCompilerStructurePlanner
{
	private static void AddResidenceCaverns(int height, WorldCompilerBand[] bands,
		List<WorldCompilerRoom> rooms, WorldCompilerSpatialIndex occupied)
	{
		int admitted = 0;
		int[] depths = [220, 400, 400, 680];
		foreach (WorldCompilerBand band in bands)
		{
			if (band.Biome is WorldCompilerBiome.Ocean or
				WorldCompilerBiome.Transition) continue;
			int bandAdmitted = 0;
			for (int depthIndex = 0; depthIndex < depths.Length; depthIndex++)
			{
				int depth = depths[depthIndex];
				bool placed = false;
				int half = WorldCompilerResidenceCavernContract.Width / 2;
				int minX = band.Left + half + 64;
				int maxX = band.Right - half - 64;
				int targetX = band.Left + band.Width * (depthIndex * 2 + 1) /
					(depths.Length * 2);
				int stride = WorldCompilerResidenceCavernContract.Width + 112;
				for (int attempt = 0; attempt < 16; attempt++)
				{
					int direction = attempt == 0 ? 0 : (attempt % 2 == 1 ? 1 : -1);
					int radius = (attempt + 1) / 2;
					int x = Math.Clamp(targetX + direction * radius * stride,
						minX, maxX);
					var bounds = new WorldCompilerRect(
						x - WorldCompilerResidenceCavernContract.Width / 2,
						height * depth / 1_000,
						WorldCompilerResidenceCavernContract.Width,
						WorldCompilerResidenceCavernContract.Height);
					if (!occupied.TryReserve(bounds, 48)) continue;
					WorldCompilerStructureKind kind =
						band.Biome == WorldCompilerBiome.TemperateForest && depth == 400 ?
						WorldCompilerStructureKind.MushroomResidenceCavern :
						WorldCompilerStructureKind.ResidenceCavern;
					rooms.Add(new WorldCompilerRoom(rooms.Count,
						kind, bounds));
					bandAdmitted++;
					admitted++;
					placed = true;
					break;
				}
				if (!placed)
					throw new InvalidOperationException(
						$"Residence cavern depth is unavailable: {band.Biome}/" +
						$"{depth}.");
			}
			if (bandAdmitted != 4)
				throw new InvalidOperationException(
					$"Residence cavern capacity is incomplete: {band.Biome}=" +
					$"{bandAdmitted}/4.");
		}
		if (admitted != 20)
			throw new InvalidOperationException(
				$"Residence cavern plan is incomplete: {admitted}/20.");
	}
}
