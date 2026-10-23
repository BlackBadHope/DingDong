using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class HardSafetyRepairSession
	{
		private bool TryMoveCoordinatedPair(List<(string Left, string Right)> conflicts, int conflictCount)
		{
			if (conflicts.Count == 0 || conflicts.Count > 8)
				return false;

			CoordinatedMove best = new(conflictCount);
			foreach ((string leftVariable, string rightVariable) in conflicts)
			{
				List<RankedLayoutCandidate> leftOptions = ViableAgainstFixed(leftVariable, rightVariable, masks, unrestrictedPools, variables, candidateIds, hardPairCompatibilityCache, working);
				List<RankedLayoutCandidate> rightOptions = ViableAgainstFixed(rightVariable, leftVariable, masks, unrestrictedPools, variables, candidateIds, hardPairCompatibilityCache, working);
				coordinatedSearches++;
				EvaluatePairOptions(leftVariable, rightVariable, leftOptions, rightOptions, conflictCount, best);
				if (coordinatedPairChecks >= ConstraintHardSafetyCoordinatedPairCheckLimit)
					break;
			}

			if (best.LeftVariable == null || best.Conflicts >= conflictCount)
				return false;
			working[best.LeftVariable] = best.LeftCandidate;
			working[best.RightVariable] = best.RightCandidate;
			return true;
		}

		private void EvaluatePairOptions(string leftVariable, string rightVariable, List<RankedLayoutCandidate> leftOptions, List<RankedLayoutCandidate> rightOptions, int conflictCount, CoordinatedMove best)
		{
			RankedLayoutCandidate originalLeft = working[leftVariable];
			RankedLayoutCandidate originalRight = working[rightVariable];
			foreach (RankedLayoutCandidate leftCandidate in leftOptions)
			{
				foreach (RankedLayoutCandidate rightCandidate in rightOptions)
				{
					if (coordinatedPairChecks >= ConstraintHardSafetyCoordinatedPairCheckLimit)
						break;
					coordinatedPairChecks++;
					if (!EvaluateHardConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, masks, candidateIds, hardPairCompatibilityCache).Compatible)
						continue;
					working[leftVariable] = leftCandidate;
					working[rightVariable] = rightCandidate;
					int nextConflicts = CountConflicts(out _, masks, variables, candidateIds, hardPairCompatibilityCache, working);
					int pairScore = leftCandidate.BaseScore + rightCandidate.BaseScore;
					if (nextConflicts < best.Conflicts || nextConflicts == best.Conflicts && nextConflicts < conflictCount && pairScore > best.Score)
						best.Set(leftVariable, rightVariable, leftCandidate, rightCandidate, nextConflicts, pairScore);
					working[leftVariable] = originalLeft;
					working[rightVariable] = originalRight;
				}
				if (coordinatedPairChecks >= ConstraintHardSafetyCoordinatedPairCheckLimit)
					break;
			}
		}

		private sealed class CoordinatedMove
		{
			public string LeftVariable;
			public string RightVariable;
			public RankedLayoutCandidate LeftCandidate;
			public RankedLayoutCandidate RightCandidate;
			public int Conflicts;
			public int Score = int.MinValue;
			public CoordinatedMove(int conflicts) => Conflicts = conflicts;
			public void Set(string left, string right, RankedLayoutCandidate leftCandidate, RankedLayoutCandidate rightCandidate, int conflicts, int score)
			{
				LeftVariable = left;
				RightVariable = right;
				LeftCandidate = leftCandidate;
				RightCandidate = rightCandidate;
				Conflicts = conflicts;
				Score = score;
			}
		}
	}
}
