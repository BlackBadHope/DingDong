using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CrossLayerRepairSession
	{
		private bool TryRepairPair(out int coordinatedMoves)
		{
			coordinatedMoves = 0;
			for (int leftIndex = 0; leftIndex < variables.Count; leftIndex++)
			{
				string leftVariable = variables[leftIndex];
				BroadLayoutLayer leftLayer = AuthoredResidencePlacementManifest.Required(leftVariable).BroadLayer;
				for (int rightIndex = leftIndex + 1; rightIndex < variables.Count; rightIndex++)
				{
					string rightVariable = variables[rightIndex];
					if (AuthoredResidencePlacementManifest.Required(rightVariable).BroadLayer == leftLayer || EvaluateConstraintPairCached(leftVariable, merged[leftVariable], rightVariable, merged[rightVariable], presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache).Compatible)
						continue;
					if (TryRepairPair(leftVariable, rightVariable, out coordinatedMoves))
						return true;
				}
			}
			return false;
		}

		private bool TryRepairPair(string leftVariable, string rightVariable, out int coordinatedMoves)
		{
			RankedLayoutCandidate originalLeft = merged[leftVariable];
			RankedLayoutCandidate originalRight = merged[rightVariable];
			List<CandidateDeploymentSite> fixedSites = FixedSites(leftVariable, rightVariable);
			List<RankedLayoutCandidate> leftOptions = ViableAcrossFixedLayers(leftVariable, rightVariable, fixedSites, presets, masks, unrestrictedPools, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged);
			List<RankedLayoutCandidate> rightOptions = ViableAcrossFixedLayers(rightVariable, leftVariable, fixedSites, presets, masks, unrestrictedPools, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged);
			foreach (RankedLayoutCandidate leftCandidate in leftOptions)
			{
				foreach (RankedLayoutCandidate rightCandidate in rightOptions)
				{
					if (!EvaluateConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache).Compatible)
						continue;
					merged[leftVariable] = leftCandidate;
					merged[rightVariable] = rightCandidate;
					string failure = CompleteFailure;
					bool accepted = AcceptComplete(merged, profiles, masks, ref failure);
					CompleteFailure = failure;
					merged[leftVariable] = originalLeft;
					merged[rightVariable] = originalRight;
					if (!accepted)
						continue;
					merged[leftVariable] = leftCandidate;
					merged[rightVariable] = rightCandidate;
					coordinatedMoves = (EqualityComparer<RankedLayoutCandidate>.Default.Equals(leftCandidate, originalLeft) ? 0 : 1) + (EqualityComparer<RankedLayoutCandidate>.Default.Equals(rightCandidate, originalRight) ? 0 : 1);
					return true;
				}
			}
			coordinatedMoves = 0;
			return false;
		}

		private List<CandidateDeploymentSite> FixedSites(string leftVariable, string rightVariable)
		{
			List<CandidateDeploymentSite> sites = new(variables.Count - 2);
			foreach (string variable in variables)
			{
				if (!string.Equals(variable, leftVariable, StringComparison.Ordinal) && !string.Equals(variable, rightVariable, StringComparison.Ordinal))
					sites.Add(merged[variable].Site);
			}
			return sites;
		}
	}
}
