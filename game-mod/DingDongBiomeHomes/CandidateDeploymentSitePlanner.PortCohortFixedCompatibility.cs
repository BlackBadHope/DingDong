using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static List<RankedLayoutCandidate> CompatibleWithFixed(string variable,
		IReadOnlyList<RankedLayoutCandidate> source,
		IReadOnlyDictionary<string, RankedLayoutCandidate> fixedAssignments,
		Dictionary<string, AuthoredSceneMutationMask> masks,
		Dictionary<RankedLayoutCandidate, int> candidateIds,
		CandidatePairCompatibilityCache hardPairCompatibilityCache, ref long pairChecks,
		ref bool budgetExhausted)
	{
		List<RankedLayoutCandidate> compatible = new();
		foreach (RankedLayoutCandidate candidate in source)
		{
			bool accepted = true;
			foreach (KeyValuePair<string, RankedLayoutCandidate> fixedEntry in fixedAssignments)
			{
				if (++pairChecks > ConstraintHardSafetyCoordinatedPairCheckLimit)
				{
					budgetExhausted = true;
					return compatible;
				}
				if (!EvaluateHardConstraintPairCached(variable, candidate,
					fixedEntry.Key, fixedEntry.Value, masks, candidateIds,
					hardPairCompatibilityCache).Compatible)
				{
					accepted = false;
					break;
				}
			}
			if (accepted) compatible.Add(candidate);
		}
		return compatible;
	}
}
