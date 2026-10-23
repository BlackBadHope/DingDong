using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static ConstraintCandidateEvaluation EvaluateHardDistributionCandidate(
		string variable, RankedLayoutCandidate candidate,
		List<CandidateDeploymentSite> planned) =>
		HardDistributionFeasible(variable, candidate, planned) ?
		ConstraintCandidateEvaluation.Accept(candidate.BaseScore) :
		ConstraintCandidateEvaluation.Reject;

	private static bool HardDistributionFeasible(string variable,
		RankedLayoutCandidate candidate, List<CandidateDeploymentSite> planned) =>
		AuthoredResidenceLayoutPolicy.TryValidateAggregateCapacity(
			AuthoredResidencePlacementManifest.Required(variable), candidate.Site,
			planned, out _);
}
