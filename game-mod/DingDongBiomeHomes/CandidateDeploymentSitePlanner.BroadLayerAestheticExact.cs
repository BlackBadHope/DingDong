using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private void RunAestheticExact(BroadLayerWork work)
		{
			Dictionary<string, RankedLayoutCandidate> hints = HintsFrom(work.Solution);
			int nodeLimit = work.Layer == BroadLayoutLayer.Surface ? 20000 : 10000;
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-exact-start|layer={work.Layer}|variables={work.Variables.Count}|values={work.DomainValues}|nodeLimit={nodeLimit}|pairCache={pairCompatibilityCache.Count}");
			work.Solved = DeterministicConstraintSolver.TrySolve(work.Variables, work.Domains, PrepareEvaluationContext,
				(variable, candidate, planned) => EvaluateCandidate(variable, candidate, planned, presets, masks, profileByNpc, rejectionReasons),
				(variable, candidate, planned) => EvaluateForward(variable, candidate, planned, profileByNpc, rejectionReasons),
				(leftVariable, leftCandidate, rightVariable, rightCandidate) => EvaluateConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache),
				assigned => AcceptLayer(assigned, masks, work.Variables), hints, enforceInitialArcConsistency: false, enforceForwardFeasibility: true, ConstraintExactArcPairEvaluationLimit, ConstraintExactForwardPairEvaluationLimit, ConstraintExactCandidateEvaluationLimit, work.ExactBudget, nodeLimit, out work.Solution, out ConstraintSolverStats<string> stats);
			work.Method = "exact";
			layerTraces.Add($"{work.Layer}:{work.Method}:solved={work.Solved}:variables={work.Variables.Count}:values={work.DomainValues}:nodes={stats.NodesVisited}:maxAssigned={stats.MaxAssigned}:bestRepair={work.RepairStats.BestConflictCount}");
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-exact-end|layer={work.Layer}|solved={work.Solved}|elapsedMs={Environment.TickCount64 - work.Started}|nodes={stats.NodesVisited}|maxAssigned={stats.MaxAssigned}/{work.Variables.Count}|bestRepair={work.RepairStats.BestConflictCount}|pairCache={pairCompatibilityCache.Count}");
		}
	}
}
