using System;
using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private void RunAestheticSolvers(BroadLayerWork work)
		{
			RunAestheticRepair(work);
			if (!work.Solved)
				RunAestheticBeam(work);
			if (!work.Solved)
				RunAestheticExact(work);
			else if (work.Method.StartsWith("repair", StringComparison.Ordinal))
			{
				layerTraces.Add($"{work.Layer}:{work.Method}:solved=True:variables={work.Variables.Count}:values={work.DomainValues}:steps={work.RepairStats.Steps}:bestRepair={work.RepairStats.BestConflictCount}");
				mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-repair-end|layer={work.Layer}|solved=True|elapsedMs={Environment.TickCount64 - work.Started}|steps={work.RepairStats.Steps}|bestRepair={work.RepairStats.BestConflictCount}|pairCache={pairCompatibilityCache.Count}");
			}
		}

		private void RunAestheticRepair(BroadLayerWork work)
		{
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-repair-start|layer={work.Layer}|variables={work.Variables.Count}|values={work.DomainValues}|timeLimitMs={work.RepairBudget}|restarts={work.RepairRestarts}|steps={work.RepairSteps}|pairCache={pairCompatibilityCache.Count}");
			work.Solved = DeterministicConstraintRepairSolver.TrySolve(work.Variables, work.Domains, PrepareEvaluationContext,
				(variable, candidate, planned) => EvaluateCandidate(variable, candidate, planned, presets, masks, profileByNpc, rejectionReasons),
				(variable, candidate, planned) => EvaluateForward(variable, candidate, planned, profileByNpc, rejectionReasons),
				(leftVariable, leftCandidate, rightVariable, rightCandidate) => EvaluateConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache),
				assigned => AcceptLayer(assigned, masks, work.Variables), PositiveHash($"layer-repair:{work.Layer}:{Main.ActiveWorldFileData?.Seed}"), work.RepairRestarts, work.RepairSteps, ConstraintRepairPairEvaluationLimit, ConstraintRepairCandidateEvaluationLimit, work.RepairBudget, out work.Solution, out work.RepairStats);
			if (work.Solution.Count == work.Variables.Count)
				work.CompleteSeed = new List<KeyValuePair<string, RankedLayoutCandidate>>(work.Solution);

			if (!work.Solved && work.RepairStats.BestConflictCount == 0)
			{
				Dictionary<string, RankedLayoutCandidate> best = new(StringComparer.Ordinal);
				foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in work.Solution)
					best[assignment.Key] = assignment.Value;
				if (AcceptLayerHardSafety(best, masks, work.Variables))
				{
					work.Solved = true;
					work.Method = "repair-hard-safety";
					mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-repair-aesthetic-fallback|layer={work.Layer}|solved=True|elapsedMs={Environment.TickCount64 - work.Started}|bestRepair=0|pairCache={pairCompatibilityCache.Count}");
				}
			}
		}
	}
}
