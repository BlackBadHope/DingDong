using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static bool IsPortCohortVariable(string variable)
	{
		AuthoredResidencePlacementContract contract =
			AuthoredResidencePlacementManifest.Required(variable);
		return contract.BroadLayer == BroadLayoutLayer.Surface
			&& contract.ClusterPolicy == ResidenceClusterPolicy.Port
			&& contract.CanShareSettlement;
	}

	private static List<CandidateDeploymentSite> PortCohortSites(
		IReadOnlyDictionary<string, RankedLayoutCandidate> fixedAssignments,
		IReadOnlyDictionary<string, RankedLayoutCandidate> assigned)
	{
		List<CandidateDeploymentSite> sites = new(
			fixedAssignments.Count + assigned.Count);
		foreach (RankedLayoutCandidate candidate in fixedAssignments.Values)
			sites.Add(candidate.Site);
		foreach (RankedLayoutCandidate candidate in assigned.Values)
			sites.Add(candidate.Site);
		return sites;
	}

	private static bool AcceptPortCohort(
		IReadOnlyDictionary<string, RankedLayoutCandidate> assigned,
		IReadOnlyDictionary<string, RankedLayoutCandidate> fixedAssignments,
		IReadOnlyList<string> variables,
		IReadOnlyDictionary<string, AuthoredSceneMutationMask> masks)
	{
		if (assigned.Count + fixedAssignments.Count != variables.Count)
			return false;
		List<CandidateDeploymentSite> sites = PortCohortSites(
			fixedAssignments, assigned);
		return AuthoredResidenceLayoutPolicy.TryAccept(sites, masks,
			out _, out _, enforceOceanBalance: false);
	}
}
