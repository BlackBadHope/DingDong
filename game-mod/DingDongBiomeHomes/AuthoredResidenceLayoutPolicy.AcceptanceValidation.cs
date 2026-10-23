using System;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private bool TryValidateDistribution(out string failure)
        {
            if (enforceAestheticDistribution && siteDensity > 0)
                return Fail($"site-density-violations={siteDensity}", out failure);
            if (enforceAestheticDistribution && megaCell > 0)
                return Fail($"mega-sector-violations={megaCell}", out failure);
            if (enforceAestheticDistribution && landmarkAdj > 0)
                return Fail($"landmark-adjacent-violations={landmarkAdj}", out failure);
            if (enforceAestheticDistribution && independent > 0)
                return Fail($"independent-density-violations={independent}", out failure);
            if (enforceAestheticDistribution && massFail > 0)
                return Fail($"sector-mass-violations={massFail}", out failure);
            if (enforceAestheticDistribution && !skyOk)
                return Fail($"sky-altitude-distribution=buckets-{distinctSky}/{skyCount},bands-{string.Join(',', skyBandCounts)},max-{maximumSkyBandCount}/{MaxSkySitesPerAltitudeBand}", out failure);
            if (!layerPopulationOk)
                return Fail($"layer-population=sky-{layerTotals[(int)BroadLayoutLayer.Sky]},surface-{layerTotals[(int)BroadLayoutLayer.Surface]},cavern-{layerTotals[(int)BroadLayoutLayer.Cavern]},underworld-{layerTotals[(int)BroadLayoutLayer.Underworld]}", out failure);
            if (enforceAestheticDistribution && cavernBands.Count < Math.Min(3, layerTotals[(int)BroadLayoutLayer.Cavern]))
                return Fail($"cavern-altitude-bands={cavernBands.Count}/{layerTotals[(int)BroadLayoutLayer.Cavern]}", out failure);
            if (altitudeViolations > 0)
                return Fail($"altitude-contract-violations={altitudeViolations}", out failure);
            if (surfaceDepthViolations > 0)
                return Fail($"surface-contact-violations={surfaceDepthViolations}", out failure);
            if (enforceAestheticDistribution && layerCoverageViolations > Math.Max(0, maximumLayerCoverageViolations))
                return Fail($"layer-coverage-violations={layerCoverageViolations}", out failure);
            if (enforceAestheticDistribution && !oceanOk)
                return Fail($"ocean-balance:left={leftOcean},right={rightOcean}", out failure);

            failure = string.Empty;
            return true;
        }

        private static bool Fail(string reason, out string failure)
        {
            failure = reason;
            return false;
        }
    }
}
