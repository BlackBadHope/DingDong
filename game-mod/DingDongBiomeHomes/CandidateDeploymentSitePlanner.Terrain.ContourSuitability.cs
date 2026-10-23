using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static readonly Point[] ContourDirections =
	{
		new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
	};

	private static bool HasCompatibleTerrainAlongExteriorContour(Rectangle area,
		AuthoredSceneMutationMask mutationMask, HomeBiome biome, bool allowWeatheredOcean = false)
	{
		HashSet<Point> observedNeighbors = new();
		int solidNeighbors = 0;
		int foreignSolidNeighbors = 0;
		int evilSolidNeighbors = 0;
		foreach (Point local in mutationMask.ExteriorContourCells)
		{
			foreach (Point direction in ContourDirections)
			{
				int localX = local.X + direction.X;
				int localY = local.Y + direction.Y;
				if (mutationMask.MutatesLocal(localX, localY))
					continue;
				int worldX = area.X + localX;
				int worldY = area.Y + localY;
				if (!WorldGen.InWorld(worldX, worldY, 1))
					return false;
				Point world = new(worldX, worldY);
				if (!observedNeighbors.Add(world))
					continue;
				Tile tile = Main.tile[worldX, worldY];
				if (!tile.HasTile || !Main.tileSolid[tile.TileType])
					continue;
				solidNeighbors++;
				if (IsEvil(tile.TileType) && !allowWeatheredOcean)
					evilSolidNeighbors++;
				if (IsForeignContourTerrain(tile.TileType, biome, allowWeatheredOcean))
					foreignSolidNeighbors++;
			}
		}
		return mutationMask.ExteriorContourCells.Count > 0 &&
			ContourCoveragePolicy.IsWithinRejectionBudget(foreignSolidNeighbors,
				solidNeighbors, ContourCoveragePolicy.MaximumForeignSolidPercent,
				minimumAllowance: 2) &&
			ContourCoveragePolicy.IsWithinRejectionBudget(evilSolidNeighbors,
				solidNeighbors, ContourCoveragePolicy.MaximumEvilSolidPercent,
				minimumAllowance: 1);
	}

	private static bool IsForeignContourTerrain(ushort type, HomeBiome biome,
		bool allowWeatheredOcean)
	{
		if (IsEvil(type))
			return !allowWeatheredOcean;
		if (biome == HomeBiome.Mushroom && type == TileID.Mud)
			return false;
		bool specialized = type == TileID.Grass ||
			MatchesBiomeTile(type, HomeBiome.Desert) ||
			MatchesBiomeTile(type, HomeBiome.Snow) ||
			MatchesBiomeTile(type, HomeBiome.Jungle) ||
			MatchesBiomeTile(type, HomeBiome.Mushroom) ||
			MatchesBiomeTile(type, HomeBiome.Hallow) ||
			MatchesBiomeTile(type, HomeBiome.Underworld);
		if (!specialized || MatchesBiomeTile(type, biome))
			return false;
		return biome != HomeBiome.Ocean || !MatchesBiomeTile(type, HomeBiome.Desert);
	}
}
