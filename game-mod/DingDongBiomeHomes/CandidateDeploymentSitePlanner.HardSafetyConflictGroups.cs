using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class HardSafetyRepairSession
	{
		private bool TryMoveConflictGroup(List<(string Left, string Right)> conflicts, int conflictCount)
		{
			if (conflicts.Count == 0)
				return false;
			foreach (IReadOnlyList<string> conflictGroup in BuildConflictGroups(conflicts, variables))
			{
				groupSearches++;
				bool solved = TryRepairConflictGroup(conflictGroup, conflictCount, out int attemptedVariables, out int attemptedDomains, out int visitedNodes, out long evaluatedPairs, masks, unrestrictedPools, variables, profileByNpc, candidateIds, hardPairCompatibilityCache, working);
				groupVariables = Math.Max(groupVariables, attemptedVariables);
				groupDomainValues = Math.Max(groupDomainValues, attemptedDomains);
				groupNodes += visitedNodes;
				groupPairChecks += evaluatedPairs;
				if (solved)
					return true;
			}
			return false;
		}
	}
}
