namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private bool TryCollectSites(out string failure)
        {
            for (int index = 0; index < sites.Count; index++)
            {
                if (!TryCollectSite(index, out failure))
                    return false;
            }

            failure = string.Empty;
            return true;
        }

        private bool TryCollectSite(int index, out string failure)
        {
            CandidateDeploymentSite site = sites[index];
            AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(site.NpcKey);
            int sector = SectorOf(site.AssetArea.Center.X);
            int layer = (int)contract.BroadLayer;
            counts[sector, layer]++;
            masses[sector, layer] += contract.Mass;
            if (contract.ScaleClass == ResidenceScaleClass.Mega)
                megas[sector, layer]++;
            if (contract.ScaleClass == ResidenceScaleClass.Landmark)
                landmarks[sector, layer]++;
            if (contract.RequiresIsolation)
                isolations[sector, layer]++;
            if (IsHighSkyCeiling(site))
                highSkyCeiling++;

            CollectAltitudeStatistics(contract, site);
            CollectOceanStatistics(contract, site);
            if (!masks.TryGetValue(site.NpcKey, out AuthoredSceneMutationMask mask))
            {
                failure = $"missing-mask:{site.NpcKey}";
                return false;
            }

            return TryInspectPairs(index, contract, site, mask, out failure);
        }
    }
}
