using System;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private void CollectAltitudeStatistics(AuthoredResidencePlacementContract contract, CandidateDeploymentSite site)
        {
            if (contract.BroadLayer == BroadLayoutLayer.Sky)
            {
                skyCount++;
                if (skyCount <= skyTops.Length)
                    skyTops[skyCount - 1] = site.AssetArea.Y;
                skyBuckets.Add(site.AssetArea.Y / 48);
                skyBands.Add(site.AltitudeBand);
                int skyBandIndex = (int)site.AltitudeBand;
                if (skyBandIndex >= 0 && skyBandIndex < skyBandCounts.Length)
                    skyBandCounts[skyBandIndex]++;
            }
            else if (contract.BroadLayer == BroadLayoutLayer.Cavern)
            {
                cavernBands.Add(site.AltitudeBand);
            }

            if (!IsAllowedLayer(contract, site.AltitudeBand))
                altitudeViolations++;
            if (contract.BroadLayer != BroadLayoutLayer.Surface)
                return;

            int depth = site.TargetReference.Y - (int)Main.worldSurface;
            maximumSurfaceDepth = Math.Max(maximumSurfaceDepth, depth);
            minimumSurfaceDepth = Math.Min(minimumSurfaceDepth, depth);
            maximumSurfaceRelief = Math.Max(maximumSurfaceRelief, Math.Max(0, site.SurfaceRelief));
            if (SurfaceContactOutOfRange(contract, depth) || SurfaceAssetTooHigh(site) || site.AltitudeBand != ResidenceAltitudeBand.Surface)
                surfaceDepthViolations++;
        }

        private void CollectOceanStatistics(AuthoredResidencePlacementContract contract, CandidateDeploymentSite site)
        {
            if (site.Biome != HomeBiome.Ocean && contract.PreferredBiome != HomeBiome.Ocean)
                return;

            ocean++;
            if (site.AssetArea.Center.X < Main.maxTilesX / 2)
                leftOcean++;
            else
                rightOcean++;
        }
    }

	private static bool IsAllowedLayer(AuthoredResidencePlacementContract contract, ResidenceAltitudeBand band)
	{
		foreach (ResidenceAltitudeBand allowed in contract.AllowedLayers)
		{
			if (allowed == band)
				return true;
		}

		return false;
	}
}
