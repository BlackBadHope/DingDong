using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		private bool HasSuitableBiomeAlongExteriorContour(Rectangle area, HomeBiome biome)
		{
			bool coastalGrade = biome == HomeBiome.Ocean &&
				profile.TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope;
			bool floatingOcean = biome == HomeBiome.Ocean && contract.BroadLayer == BroadLayoutLayer.Sky;
			bool waterlineOcean = biome == HomeBiome.Ocean &&
				profile.TargetAnchor == DeploymentTargetAnchor.CoastalWaterlineEnvelope;
			int previousX = int.MinValue;
			int columns = 0;
			int matchingColumns = 0;
			foreach (Point local in mutationMask.ExteriorContourCells)
			{
				if (local.X == previousX)
					continue;
				previousX = local.X;
				columns++;
				int x = area.X + local.X;
				// Airborne ocean residences need coastal coverage, not a shallow
				// seabed below every column. The target anchor still checks real
				// water/biome admission, and the same coverage threshold is retained.
				bool matches = floatingOcean
					? x >= 0 && x < Terraria.Main.maxTilesX && (x <= Terraria.Main.maxTilesX / 6 || x >= Terraria.Main.maxTilesX * 5 / 6)
					// Ports follow the validated open ocean surface. Requiring the
					// ground-biome index here concentrates every port on a shallow
					// shelf and excludes the deep-water columns admitted above.
					: waterlineOcean ? surfaceIndex.TryWaterline(x, out _)
					: surfaceIndex.MatchesContourColumn(x, biome, coastalGrade);
				if (matches)
					matchingColumns++;
			}
			return ContourCoveragePolicy.HasMinimumCoverage(matchingColumns, columns,
				ContourCoveragePolicy.MinimumBiomeCoveragePercent);
		}

		private bool HasCompatibleExteriorNeighbors(Rectangle area, HomeBiome biome)
			=> HasCompatibleTerrainAlongExteriorContour(area, mutationMask, biome,
				allowWeatheredOcean: biome == HomeBiome.Ocean &&
					profile.TargetAnchor is DeploymentTargetAnchor.SurfaceTerrainEnvelope or
						DeploymentTargetAnchor.CoastalWaterlineEnvelope);
	}
}
