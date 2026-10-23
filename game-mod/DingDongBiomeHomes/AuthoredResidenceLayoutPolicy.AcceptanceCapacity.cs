using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private void EvaluateSectorCapacity()
        {
            for (int sector = 0; sector < MacroSectorCount; sector++)
            {
                for (int layer = 0; layer < 4; layer++)
                {
                    if (counts[sector, layer] > MaxSitesPerSector((BroadLayoutLayer)layer))
                        siteDensity++;
                    if (megas[sector, layer] > MaxMegaPerSectorLayer)
                        megaCell++;
                    if (isolations[sector, layer] > MaxIndependentPerSector((BroadLayoutLayer)layer))
                        independent++;
                    if (masses[sector, layer] > MaxSectorMass)
                        massFail++;
                    if (sector + 1 < MacroSectorCount)
                        EvaluateAdjacentLandmarks(sector, (BroadLayoutLayer)layer);
                }
            }
        }

        private void EvaluateAdjacentLandmarks(int sector, BroadLayoutLayer layer)
        {
            int sameBiome = 0;
            foreach (HomeBiome biome in Enum.GetValues<HomeBiome>())
            {
                int pair = LandmarkMegaCount(sites, sector, layer, biome) + LandmarkMegaCount(sites, sector + 1, layer, biome);
                if (pair > sameBiome)
                    sameBiome = pair;
            }

            if (sameBiome > MaxAdjacentLandmarkMega)
                landmarkAdj++;
        }
    }

	private static int LandmarkMegaCount(IReadOnlyList<CandidateDeploymentSite> sites, int sector, BroadLayoutLayer layer, HomeBiome? biome)
	{
		int count = 0;
		foreach (CandidateDeploymentSite site in sites)
		{
			AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(site.NpcKey);
			if (contract.BroadLayer != layer || SectorOf(site.AssetArea.Center.X) != sector)
				continue;
			if (biome.HasValue && contract.PreferredBiome != biome.Value)
				continue;
			if (contract.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega)
				count++;
		}

		return count;
	}

	private static int LandmarkMegaCount(IReadOnlyList<CandidateDeploymentSite> sites, int sector, BroadLayoutLayer layer) => LandmarkMegaCount(sites, sector, layer, biome: null);
}
