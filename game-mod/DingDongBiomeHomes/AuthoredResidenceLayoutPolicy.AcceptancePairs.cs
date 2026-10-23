namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
    private sealed partial class LayoutAcceptanceSession
    {
        private bool TryInspectPairs(
            int leftIndex,
            AuthoredResidencePlacementContract contract,
            CandidateDeploymentSite site,
            AuthoredSceneMutationMask mask,
            out string failure)
        {
            for (int rightIndex = leftIndex + 1; rightIndex < sites.Count; rightIndex++)
            {
                CandidateDeploymentSite otherSite = sites[rightIndex];
                AuthoredResidencePlacementContract other = AuthoredResidencePlacementManifest.Required(otherSite.NpcKey);
                if (!masks.TryGetValue(otherSite.NpcKey, out AuthoredSceneMutationMask otherMask))
                {
                    failure = $"missing-mask:{otherSite.NpcKey}";
                    return false;
                }

                // Physical conflicts are never aesthetic spacing-budget exceptions.
                if (site.ProtectedArea.Intersects(otherSite.ProtectedArea) ||
                    mask.IntersectsTranslated(site.AssetArea.Location, otherMask, otherSite.AssetArea.Location))
                {
                    failure = $"physical-envelope-overlap:{site.NpcKey}/{otherSite.NpcKey}";
                    return false;
                }

                if (TryValidateHardPair(contract, mask, site, other, otherMask, otherSite, out string pairReason))
                    continue;
                CountPairViolation(mask, site, otherMask, otherSite, pairReason);
            }

            failure = string.Empty;
            return true;
        }

        private void CountPairViolation(
            AuthoredSceneMutationMask mask,
            CandidateDeploymentSite site,
            AuthoredSceneMutationMask otherMask,
            CandidateDeploymentSite otherSite,
            string pairReason)
        {
            if (pairReason == "isolated-envelope")
            {
                if (allowDisjointFocusOverlap && !mask.IntersectsTranslated(site.AssetArea.Location, otherMask, otherSite.AssetArea.Location))
                    spacing++;
                else
                    isolatedEnvelope++;
            }
            else if (pairReason == "landmark-mega-sector")
            {
                landmarkMegaCell++;
            }
            else if (pairReason == "landmark-mega-stack")
            {
                landmarkMegaStack++;
            }
            else
            {
                spacing++;
            }
        }
    }
}
