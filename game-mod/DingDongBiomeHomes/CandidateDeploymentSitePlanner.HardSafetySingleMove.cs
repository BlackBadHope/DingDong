using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class HardSafetyRepairSession
	{
		private bool TryMoveSingle(List<(string Left, string Right)> conflicts, int conflictCount)
		{
			HashSet<string> conflictedVariables = new(StringComparer.Ordinal);
			foreach ((string left, string right) in conflicts)
			{
				conflictedVariables.Add(left);
				conflictedVariables.Add(right);
			}

			string bestVariable = null;
			RankedLayoutCandidate bestCandidate = null;
			int bestTotalConflicts = conflictCount;
			int bestScore = int.MinValue;
			foreach (string variable in variables)
			{
				if (!conflictedVariables.Contains(variable))
					continue;
				int unaffectedConflicts = conflictCount - ConflictsTouching(variable, conflicts);
				RankedLayoutCandidate current = working[variable];
				foreach (RankedLayoutCandidate candidate in unrestrictedPools[variable])
				{
					if (EqualityComparer<RankedLayoutCandidate>.Default.Equals(candidate, current))
						continue;
					int total = unaffectedConflicts + ConflictsForCandidate(variable, candidate);
					if (total < bestTotalConflicts || total == bestTotalConflicts && total < conflictCount && candidate.BaseScore > bestScore)
					{
						bestVariable = variable;
						bestCandidate = candidate;
						bestTotalConflicts = total;
						bestScore = candidate.BaseScore;
					}
				}
			}

			if (bestVariable == null)
				return false;
			working[bestVariable] = bestCandidate;
			singleMoves++;
			return true;
		}

		private static int ConflictsTouching(string variable, List<(string Left, string Right)> conflicts)
		{
			int count = 0;
			foreach ((string left, string right) in conflicts)
			{
				if (string.Equals(left, variable, StringComparison.Ordinal) || string.Equals(right, variable, StringComparison.Ordinal))
					count++;
			}
			return count;
		}

		private int ConflictsForCandidate(string variable, RankedLayoutCandidate candidate)
		{
			int count = 0;
			foreach (string other in variables)
			{
				if (!string.Equals(other, variable, StringComparison.Ordinal) && !EvaluateHardConstraintPairCached(variable, candidate, other, working[other], masks, candidateIds, hardPairCompatibilityCache).Compatible)
					count++;
			}
			return count;
		}
	}
}
