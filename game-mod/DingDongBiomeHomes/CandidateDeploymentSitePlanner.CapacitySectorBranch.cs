using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CapacitySectorSearch
	{
		private void BeginAssignment(int depth, int selectedIndex, CapacitySectorItem item, CapacitySectorOption option)
		{
			Place(item, option.Sector, 1, counts, masses, isolations, megas, landmarks, ref oceanLeft, ref oceanRight);
			assigned[selectedIndex] = true;
			assignedSectors.Add(item.Variable, option.Sector);
			maxAssigned = System.Math.Max(maxAssigned, depth + 1);
		}

		private void EndAssignment(int selectedIndex, CapacitySectorItem item, CapacitySectorOption option)
		{
			assignedSectors.Remove(item.Variable);
			assigned[selectedIndex] = false;
			Place(item, option.Sector, -1, counts, masses, isolations, megas, landmarks, ref oceanLeft, ref oceanRight);
		}

		private bool TryPrepareBranch(int selectedIndex, CapacitySectorItem selectedItem, CapacitySectorOption option)
		{
			bool branchConsistent = TryRestrictActiveDomain(selectedIndex, option.Sector, out activeDomains, items, activeDomains);
			if (branchConsistent && activeDomainsArcConsistent)
				branchConsistent = MaintainArcConsistency(selectedItem);

			if (branchConsistent && !CanStillMeetRequiredLayerCoverage(items, counts, masses, isolations, megas, landmarks, assigned, activeDomains))
			{
				coverageForwardRejectedBranches++;
				branchConsistent = false;
			}
			return branchConsistent;
		}

		private bool MaintainArcConsistency(CapacitySectorItem selectedItem)
		{
			int remainingArcBudget = ConstraintSectorArcPairEvaluationLimit - arcPairEvaluations;
			if (remainingArcBudget <= 0)
			{
				arcBudgetExhausted = true;
				activeDomainsArcConsistent = false;
				return true;
			}

			macChecks++;
			bool consistent = DeterministicArcConsistencyPruner.TryPruneAfterDomainRestriction(variables, activeDomains, pairCompatible, remainingArcBudget, new[] { selectedItem.Variable }, out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> maintainedDomains, out ArcConsistencyPruneStats<string> stats);
			AccumulateArcStats(stats, ref arcPairEvaluations, ref arcRevisions, ref arcValuesRemoved, ref arcBudgetExhausted);
			activeDomains = maintainedDomains;
			activeDomainsArcConsistent = consistent && !stats.BudgetExhausted;
			if (!consistent)
			{
				macRejectedBranches++;
				arcWipeoutWitness = $"{stats.BlockedVariable}>{stats.SupportVariable}";
			}
			return consistent;
		}
	}
}
