using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
	public static bool TryAccept(IReadOnlyList<CandidateDeploymentSite> sites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> masks, out string failure, out string report, bool requireFullLayerPopulation = true, int maximumLayerCoverageViolations = 0, int minimumDistinctSkyBands = 2, bool enforceAestheticDistribution = true, int maximumVisualSpacingViolations = 0, bool allowDisjointFocusOverlap = false, bool enforceOceanBalance = true)
	{
		LayoutAcceptanceSession session = new(sites, masks, requireFullLayerPopulation, maximumLayerCoverageViolations, minimumDistinctSkyBands, enforceAestheticDistribution, maximumVisualSpacingViolations, allowDisjointFocusOverlap, enforceOceanBalance);
		return session.TryAccept(out failure, out report);
	}
}
