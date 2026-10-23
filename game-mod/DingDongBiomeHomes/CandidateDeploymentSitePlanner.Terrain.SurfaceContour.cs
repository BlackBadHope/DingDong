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
			int previousX = int.MinValue;
			int columns = 0;
			int matchingColumns = 0;
			foreach (Point local in mutationMask.ExteriorContourCells)
			{
				if (local.X == previousX)
					continue;
				previousX = local.X;
				columns++;
				if (surfaceIndex.MatchesContourColumn(area.X + local.X, biome, coastalGrade))
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
