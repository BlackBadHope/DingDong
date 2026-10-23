using System;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private void EvaluateDistribution()
        {
            distinctSky = skyBuckets.Count;
            maximumSkyBandCount = Math.Max(skyBandCounts[0], Math.Max(skyBandCounts[1], skyBandCounts[2]));
            int requiredSkyBands = Math.Clamp(minimumDistinctSkyBands, 1, 3);
            skyOk = skyCount < 3 || distinctSky >= 3 && skyBands.Count >= requiredSkyBands && maximumSkyBandCount <= MaxSkySitesPerAltitudeBand;
			oceanOk = !enforceOceanBalance || ocean < 3 || leftOcean > 0 && rightOcean > 0;

            for (int layer = 0; layer < 4; layer++)
                EvaluateLayerDistribution(layer);

            layerPopulationOk = !requireFullLayerPopulation ||
                layerTotals[(int)BroadLayoutLayer.Surface] > sites.Count / 2 &&
                layerTotals[(int)BroadLayoutLayer.Sky] >= MinimumSkySites &&
                layerTotals[(int)BroadLayoutLayer.Cavern] >= MinimumCavernSites &&
                layerTotals[(int)BroadLayoutLayer.Underworld] >= MinimumUnderworldSites;
        }

        private void EvaluateLayerDistribution(int layer)
        {
            for (int sector = 0; sector < MacroSectorCount; sector++)
            {
                int value = counts[sector, layer];
                layerTotals[layer] += value;
                if (value <= 0)
                    continue;
                layerSectors[layer]++;
                if (sector < MacroSectorCount / 2)
                    layerLeft[layer] += value;
                else
                    layerRight[layer] += value;
            }

            BroadLayoutLayer broadLayer = (BroadLayoutLayer)layer;
            if (layerSectors[layer] < RequiredDistinctSectors(broadLayer, layerTotals[layer]))
                layerCoverageViolations++;
			// Two residences already have to occupy distinct macro sectors. Requiring
			// that minimal pair to cross the entire world is not a density rule; it
			// makes narrow authored biomes (notably the x10 Underworld pair) falsely
			// unsatisfiable. Cross-world balance becomes meaningful at three sites.
			if (layerTotals[layer] >= 3 && (layerLeft[layer] == 0 || layerRight[layer] == 0))
				layerCoverageViolations++;
        }
    }
}
