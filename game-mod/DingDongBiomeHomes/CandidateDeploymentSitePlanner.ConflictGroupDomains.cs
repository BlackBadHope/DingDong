using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConflictGroupRepairSearch
	{
		private bool TryBuildDomains(List<string> movingVariables, HashSet<string> portVariables, bool balancePort, Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> domains, Dictionary<string, int> blockerCounts)
		{
			bool allNonEmpty = true;
			foreach (string variable in movingVariables)
			{
				List<RankedLayoutCandidate> viable = CandidatesCompatibleWithFixed(variable, blockerCounts);
				if (budgetExhausted)
					return false;
				if (viable.Count == 0)
					allNonEmpty = false;
				MoveCurrentCandidateFirst(variable, viable);
				if (balancePort && portVariables.Contains(variable) && viable.Count > ConstraintHardSafetyPortCohortDomainLimit)
					viable = BalancePortCandidates(variable, viable);
				domains.Add(variable, viable);
				attemptedDomains += viable.Count;
			}
			return allNonEmpty;
		}

		private List<RankedLayoutCandidate> CandidatesCompatibleWithFixed(string variable, Dictionary<string, int> blockerCounts)
		{
			List<RankedLayoutCandidate> viable = new();
			foreach (RankedLayoutCandidate candidate in unrestrictedPools[variable])
			{
				bool compatible = true;
				foreach (string fixedVariable in variables)
				{
					if (moving.Contains(fixedVariable))
						continue;
					if (++localPairChecks > ConstraintHardSafetyCoordinatedPairCheckLimit)
					{
						budgetExhausted = true;
						compatible = false;
						break;
					}
					if (!EvaluateHardConstraintPairCached(variable, candidate, fixedVariable, working[fixedVariable], masks, candidateIds, hardPairCompatibilityCache).Compatible)
					{
						compatible = false;
						if (!blockerCounts.TryAdd(fixedVariable, 1))
							blockerCounts[fixedVariable]++;
					}
				}
				if (compatible)
					viable.Add(candidate);
				if (budgetExhausted)
					break;
			}
			return viable;
		}

		private void MoveCurrentCandidateFirst(string variable, List<RankedLayoutCandidate> viable)
		{
			int currentIndex = viable.FindIndex(candidate => EqualityComparer<RankedLayoutCandidate>.Default.Equals(candidate, working[variable]));
			if (currentIndex <= 0)
				return;
			RankedLayoutCandidate current = viable[currentIndex];
			viable.RemoveAt(currentIndex);
			viable.Insert(0, current);
		}
	}
}
