using System;
using System.Text;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private string BuildReport()
        {
            StringBuilder summary = new();
            summary.Append($"planned={sites.Count}/61");
            summary.Append($"|skyCount={skyCount}|skyBuckets={distinctSky}|skyBands={skyBands.Count}|skyBandCounts={string.Join(',', skyBandCounts)}|skyBandMax={maximumSkyBandCount}/{MaxSkySitesPerAltitudeBand}|skyTops={string.Join(',', skyTops[..Math.Min(skyCount, skyTops.Length)])}");
            summary.Append($"|layerCounts={string.Join(',', layerTotals)}|layerSectors={string.Join(',', layerSectors)}|layerSides={string.Join(',', layerLeft)}/{string.Join(',', layerRight)}|cavernBands={cavernBands.Count}");
            summary.Append($"|layerPopulationOk={layerPopulationOk}|layerPopulationScope={(requireFullLayerPopulation ? "full" : "component")}|layerMinimums=surface-majority,sky-{MinimumSkySites},cavern-{MinimumCavernSites},underworld-{MinimumUnderworldSites}");
            summary.Append($"|surfaceDepthMax={(maximumSurfaceDepth == int.MinValue ? -1 : maximumSurfaceDepth)}|surfaceDepthMin={(minimumSurfaceDepth == int.MaxValue ? 1 : minimumSurfaceDepth)}|surfaceReliefMax={maximumSurfaceRelief}|surfaceDepthViolations={surfaceDepthViolations}|altitudeViolations={altitudeViolations}|layerCoverageViolations={layerCoverageViolations}");
            summary.Append($"|highSkyCeiling={highSkyCeiling}/{MaxHighSkySitesAtCeiling}|isolatedEnvelope={isolatedEnvelope}|landmarkMegaCell={landmarkMegaCell}|landmarkMegaStack={landmarkMegaStack}");
			summary.Append($"|ocean={ocean}|leftOcean={leftOcean}|rightOcean={rightOcean}|oceanBalanceRequired={enforceOceanBalance}");
            summary.Append($"|spacingViolations={spacing}|siteDensity={siteDensity}|megaCell={megaCell}|landmarkAdj={landmarkAdj}|independent={independent}|massFail={massFail}");
            return summary.ToString();
        }
    }
}
