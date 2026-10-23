using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CrossLayerRepairSession
	{
		private void ConsiderSingleMove(string variable, RankedLayoutCandidate current, RankedLayoutCandidate candidate, int currentConflicts, int remainingConflicts, List<CandidateDeploymentSite> plannedWithoutVariable, CrossLayerMove best)
		{
			if (EqualityComparer<RankedLayoutCandidate>.Default.Equals(candidate, current) || !PairsAccept(variable, candidate) || !EvaluateForward(variable, candidate, plannedWithoutVariable, profileByNpc, rejectionReasons))
				return;

			ConstraintCandidateEvaluation evaluation = EvaluateCandidate(variable, candidate, plannedWithoutVariable, presets, masks, profileByNpc, rejectionReasons);
			if (!evaluation.Feasible)
				return;

			merged[variable] = candidate;
			bool layerAccepted = AcceptVariableLayer(variable);
			int candidateConflicts = layerAccepted
				? CrossConflictsFor(variable, candidate, presets, masks, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged)
				: int.MaxValue;
			merged[variable] = current;
			if (!layerAccepted || candidateConflicts >= currentConflicts)
				return;

			int totalConflicts = remainingConflicts - currentConflicts + candidateConflicts;
			if (totalConflicts < best.TotalConflicts || totalConflicts == best.TotalConflicts && evaluation.Score > best.Score)
			{
				best.TotalConflicts = totalConflicts;
				best.Score = evaluation.Score;
				best.Variable = variable;
				best.Candidate = candidate;
			}
		}

		private bool PairsAccept(string variable, RankedLayoutCandidate candidate)
		{
			foreach (string other in variables)
			{
				if (!string.Equals(other, variable, StringComparison.Ordinal) && !EvaluateConstraintPairCached(variable, candidate, other, merged[other], presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache).Compatible)
					return false;
			}
			return true;
		}

		private bool AcceptVariableLayer(string variable)
		{
			BroadLayoutLayer layer = AuthoredResidencePlacementManifest.Required(variable).BroadLayer;
			List<CandidateDeploymentSite> layerSites = new();
			foreach (string layerVariable in variables)
			{
				if (AuthoredResidencePlacementManifest.Required(layerVariable).BroadLayer == layer)
					layerSites.Add(merged[layerVariable].Site);
			}
			return AuthoredResidenceLayoutPolicy.TryAccept(layerSites, masks, out _, out _,
				requireFullLayerPopulation: false, enforceOceanBalance: false);
		}
	}
}
