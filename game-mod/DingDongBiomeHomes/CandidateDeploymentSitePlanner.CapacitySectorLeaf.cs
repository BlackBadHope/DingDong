using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CapacitySectorSearch
	{
		private bool TryAcceptLeaf()
		{
			if (RequiresBilateralOcean && (oceanLeft == 0 || oceanRight == 0))
			{
				leafOceanRejectedPlans++;
				return false;
			}
			if (!HasRequiredLayerCoverage(counts))
			{
				leafCoverageRejectedPlans++;
				return false;
			}

			arcPlans++;
			if (activeDomainsArcConsistent)
				return TryAcceptDomains(SnapshotActiveDomains(activeDomains));

			int remainingArcBudget = ConstraintSectorArcPairEvaluationLimit - arcPairEvaluations;
			if (remainingArcBudget <= 0)
			{
				return TryAcceptDomains(SnapshotActiveDomains(activeDomains));
			}

			bool consistent = DeterministicArcConsistencyPruner.TryPrune(variables, activeDomains, pairCompatible, remainingArcBudget, out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> pruned, out ArcConsistencyPruneStats<string> stats);
			AccumulateArcStats(stats, ref arcPairEvaluations, ref arcRevisions, ref arcValuesRemoved, ref arcBudgetExhausted);
			if (!consistent)
			{
				arcRejectedPlans++;
				arcWipeoutWitness = $"{stats.BlockedVariable}>{stats.SupportVariable}";
				return false;
			}
			return TryAcceptDomains(pruned);
		}

		private bool TryAcceptDomains(Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> candidateDomains)
		{
			CapacityDomainAdmission admission = acceptRestrictedPools(candidateDomains);
			if (admission.Accepted)
			{
				acceptedRestrictedPools = candidateDomains;
				return true;
			}
			downstreamRejectedPlans++;
			if (admission.BudgetExhausted)
			{
				downstreamAdmissionBudgetExhausted = true;
				budgetExhausted = true;
			}
			return false;
		}
	}
}
