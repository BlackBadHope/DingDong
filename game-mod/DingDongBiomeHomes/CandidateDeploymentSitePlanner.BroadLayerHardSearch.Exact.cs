using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private void RunHardExact(BroadLayerWork work)
		{
			Dictionary<string, RankedLayoutCandidate> hints = HintsFrom(work.Solution);
			int nodeLimit = work.Layer == BroadLayoutLayer.Surface ? 40000 : 20000;
			long started = Environment.TickCount64;
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-hard-exact-start|layer={work.Layer}|variables={work.Variables.Count}|values={work.DomainValues}|nodeLimit={nodeLimit}|timeLimitMs={work.ExactBudget}|hardPairCache={hardPairCompatibilityCache.Count}");
			work.Solved = DeterministicConstraintSolver.TrySolve(work.Variables, work.Domains, PrepareEvaluationContext,
				EvaluateHardDistributionCandidate, HardDistributionFeasible,
				(leftVariable, leftCandidate, rightVariable, rightCandidate) => EvaluateHardConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, masks, candidateIds, hardPairCompatibilityCache),
				assigned => AcceptLayerHardSafety(assigned, masks, work.Variables), hints, enforceInitialArcConsistency: false, enforceForwardFeasibility: false, ConstraintExactArcPairEvaluationLimit, ConstraintExactForwardPairEvaluationLimit, ConstraintExactCandidateEvaluationLimit, work.ExactBudget, nodeLimit, out work.Solution, out ConstraintSolverStats<string> stats);
			work.Method = "layer-hard-exact";
			layerTraces.Add($"{work.Layer}:{work.Method}:solved={work.Solved}:variables={work.Variables.Count}:values={work.DomainValues}:nodes={stats.NodesVisited}:maxAssigned={stats.MaxAssigned}");
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-hard-exact-end|layer={work.Layer}|solved={work.Solved}|elapsedMs={Environment.TickCount64 - started}|nodes={stats.NodesVisited}|maxAssigned={stats.MaxAssigned}/{work.Variables.Count}|hardPairCache={hardPairCompatibilityCache.Count}");
		}
	}
}
