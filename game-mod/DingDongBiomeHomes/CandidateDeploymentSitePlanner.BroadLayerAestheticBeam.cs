using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private void RunAestheticBeam(BroadLayerWork work)
		{
			Dictionary<string, RankedLayoutCandidate> hints = HintsFrom(work.Solution);
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-beam-start|layer={work.Layer}|variables={work.Variables.Count}|values={work.DomainValues}|width={ConstraintBeamWidth}|fanout={ConstraintBeamCandidateFanout}|pairCache={pairCompatibilityCache.Count}");
			work.Solved = DeterministicConstraintBeamSolver.TrySolve(work.Variables, work.Domains, PrepareEvaluationContext,
				(variable, candidate, planned) => EvaluateCandidate(variable, candidate, planned, presets, masks, profileByNpc, rejectionReasons),
				(leftVariable, leftCandidate, rightVariable, rightCandidate) => EvaluateConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache),
				assigned => AcceptLayer(assigned, masks, work.Variables), hints, ConstraintBeamWidth, ConstraintBeamCandidateFanout, ConstraintBeamStatesPerDiversityBucket, ConstraintBeamPairEvaluationLimit, ConstraintBeamCandidateEvaluationLimit, out work.Solution, out ConstraintBeamStats<string> stats);
			work.Method = "beam";
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-beam-end|layer={work.Layer}|solved={work.Solved}|elapsedMs={Environment.TickCount64 - work.Started}|depth={stats.MaxDepth}/{work.Variables.Count}|expanded={stats.ExpandedStates}|dead={stats.DeadStates}|bestRepair={work.RepairStats.BestConflictCount}|pairCache={pairCompatibilityCache.Count}");
			if (work.Solved)
				layerTraces.Add($"{work.Layer}:{work.Method}:solved=True:variables={work.Variables.Count}:values={work.DomainValues}:depth={stats.MaxDepth}:bestRepair={work.RepairStats.BestConflictCount}");
		}

		private static Dictionary<string, RankedLayoutCandidate> HintsFrom(List<KeyValuePair<string, RankedLayoutCandidate>> solution)
		{
			Dictionary<string, RankedLayoutCandidate> hints = new(StringComparer.Ordinal);
			foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in solution)
				hints[assignment.Key] = assignment.Value;
			return hints;
		}
	}
}
