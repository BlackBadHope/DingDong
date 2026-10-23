using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CrossLayerRepairSession
	{
		public bool TryRepair(out int moves, out int remainingConflicts)
		{
			moves = 0;
			remainingConflicts = CountCrossLayerConflicts(presets, masks, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged);
			for (int round = 0; round < 16 && remainingConflicts > 0; round++)
			{
				CrossLayerMove best = FindBestSingleMove(remainingConflicts);
				if (best.Variable == null)
					break;
				merged[best.Variable] = best.Candidate;
				moves++;
				remainingConflicts = best.TotalConflicts;
			}

			if (remainingConflicts > 0 && TryRepairPair(out int coordinatedMoves))
			{
				moves += coordinatedMoves;
				remainingConflicts = CountCrossLayerConflicts(presets, masks, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged);
			}

			string failure = CompleteFailure;
			bool solved = remainingConflicts == 0 && AcceptComplete(merged, profiles, masks, ref failure);
			CompleteFailure = failure;
			return solved;
		}

		private CrossLayerMove FindBestSingleMove(int remainingConflicts)
		{
			CrossLayerMove best = new() { TotalConflicts = remainingConflicts, Score = int.MinValue };
			foreach (string variable in variables)
			{
				RankedLayoutCandidate current = merged[variable];
				int currentConflicts = CrossConflictsFor(variable, current, presets, masks, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged);
				if (currentConflicts == 0)
					continue;
				List<CandidateDeploymentSite> plannedWithoutVariable = SitesWithout(variable);
				foreach (RankedLayoutCandidate candidate in unrestrictedPools[variable])
					ConsiderSingleMove(variable, current, candidate, currentConflicts, remainingConflicts, plannedWithoutVariable, best);
			}
			return best;
		}

		private List<CandidateDeploymentSite> SitesWithout(string excludedVariable)
		{
			List<CandidateDeploymentSite> sites = new(variables.Count - 1);
			foreach (string variable in variables)
			{
				if (variable != excludedVariable)
					sites.Add(merged[variable].Site);
			}
			return sites;
		}

		private sealed class CrossLayerMove
		{
			public string Variable;
			public RankedLayoutCandidate Candidate;
			public int TotalConflicts;
			public int Score;
		}
	}
}
