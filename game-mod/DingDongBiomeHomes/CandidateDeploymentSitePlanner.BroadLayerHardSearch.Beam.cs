using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private void RunHardBeam(BroadLayerWork work)
		{
			Dictionary<string, RankedLayoutCandidate> hints = HintsFrom(work.Solution);
			long started = Environment.TickCount64;
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-hard-beam-start|layer={work.Layer}|variables={work.Variables.Count}|values={work.DomainValues}|width={ConstraintBeamWidth}|fanout={ConstraintBeamCandidateFanout}|hardPairCache={hardPairCompatibilityCache.Count}");
			work.Solved = DeterministicConstraintBeamSolver.TrySolve(work.Variables, work.Domains, PrepareEvaluationContext,
				EvaluateHardDistributionCandidate,
				(leftVariable, leftCandidate, rightVariable, rightCandidate) => EvaluateHardConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, masks, candidateIds, hardPairCompatibilityCache),
				assigned => AcceptLayerHardSafety(assigned, masks, work.Variables), hints, ConstraintBeamWidth, ConstraintBeamCandidateFanout, ConstraintBeamStatesPerDiversityBucket, ConstraintBeamPairEvaluationLimit, ConstraintBeamCandidateEvaluationLimit, out work.Solution, out ConstraintBeamStats<string> stats);
			if (work.Solution.Count == work.Variables.Count)
				work.CompleteSeed = new List<KeyValuePair<string, RankedLayoutCandidate>>(work.Solution);
			work.Method = "layer-hard-beam";
			layerTraces.Add($"{work.Layer}:{work.Method}:solved={work.Solved}:variables={work.Variables.Count}:values={work.DomainValues}:depth={stats.MaxDepth}:expanded={stats.ExpandedStates}:dead={stats.DeadStates}");
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-hard-beam-end|layer={work.Layer}|solved={work.Solved}|elapsedMs={Environment.TickCount64 - started}|depth={stats.MaxDepth}/{work.Variables.Count}|expanded={stats.ExpandedStates}|dead={stats.DeadStates}|hardPairCache={hardPairCompatibilityCache.Count}");
		}
	}
}
