using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static bool CrossLayerPairsAccept(IReadOnlyList<string> variables,
		IReadOnlyDictionary<string, RankedLayoutCandidate> assigned,
		Func<string, RankedLayoutCandidate, string, RankedLayoutCandidate,
			ConstraintPairCompatibilityEvaluation> pairCompatible,
		ref long pairLookups, ref int pairEvaluations)
	{
		for (int leftIndex = 0; leftIndex < variables.Count; leftIndex++)
		{
			string left = variables[leftIndex];
			BroadLayoutLayer leftLayer =
				AuthoredResidencePlacementManifest.Required(left).BroadLayer;
			for (int rightIndex = leftIndex + 1; rightIndex < variables.Count; rightIndex++)
			{
				string right = variables[rightIndex];
				if (AuthoredResidencePlacementManifest.Required(right).BroadLayer == leftLayer)
					continue;
				ConstraintPairCompatibilityEvaluation evaluation = pairCompatible(left,
					assigned[left], right, assigned[right]);
				pairLookups++;
				if (!evaluation.CacheHit)
					pairEvaluations++;
				if (!evaluation.Compatible)
					return false;
			}
		}
		return true;
	}
}
