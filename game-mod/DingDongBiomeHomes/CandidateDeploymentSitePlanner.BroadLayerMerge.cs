using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private bool FinalizeMergedSolution(out List<KeyValuePair<string, RankedLayoutCandidate>> componentSolution, out string componentTrace)
		{
			int repairMoves = 0;
			int crossConflicts = merged.Count == variables.Count
				? CountCrossLayerConflicts(presets, masks, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged)
				: int.MaxValue;
			string failure = CompleteFailure;
			if (merged.Count == variables.Count && crossConflicts > 0)
			{
				mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-merge-repair-start|crossConflicts={crossConflicts}|pairCache={pairCompatibilityCache.Count}");
				TryRepairCrossLayerMerge(out repairMoves, out crossConflicts, profiles, presets, masks, unrestrictedPools, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, ref failure, merged);
				mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-merge-repair-end|moves={repairMoves}|crossConflicts={crossConflicts}|pairCache={pairCompatibilityCache.Count}");
			}

			CompleteFailure = failure;
			if (crossConflicts > 0)
				failure = $"cross-layer-conflicts:{crossConflicts}";
			if (merged.Count != variables.Count || crossConflicts > 0 || !AcceptComplete(merged, profiles, masks, ref failure))
			{
				CompleteFailure = failure;
				componentSolution = SolutionInVariableOrder();
				componentTrace = $"{string.Join(',', layerTraces)},merged={merged.Count}/{variables.Count},mergeRepairMoves={repairMoves},mergeCrossConflicts={crossConflicts},final={CompleteFailure}";
				return false;
			}

			CompleteFailure = failure;
			componentSolution = SolutionInVariableOrder();
			componentTrace = $"{string.Join(',', layerTraces)},merged={merged.Count}/{variables.Count},mergeRepairMoves={repairMoves},mergeCrossConflicts={crossConflicts},final=PASS";
			return true;
		}

		private List<KeyValuePair<string, RankedLayoutCandidate>> SolutionInVariableOrder()
		{
			List<KeyValuePair<string, RankedLayoutCandidate>> solution = new(merged.Count);
			if (merged.Count == variables.Count)
			{
				foreach (string variable in variables)
					solution.Add(new KeyValuePair<string, RankedLayoutCandidate>(variable, merged[variable]));
			}
			return solution;
		}
	}
}
