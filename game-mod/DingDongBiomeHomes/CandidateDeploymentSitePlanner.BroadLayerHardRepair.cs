using System;
using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private void RunHardSafetySolvers(BroadLayerWork work)
		{
			RunHardRepair(work);
			if (!work.Solved)
				RunHardBeam(work);
			if (!work.Solved)
				RunHardExact(work);
		}

		private void RunHardRepair(BroadLayerWork work)
		{
			long started = Environment.TickCount64;
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-hard-safety-start|layer={work.Layer}|variables={work.Variables.Count}|values={work.DomainValues}|timeLimitMs={work.RepairBudget}|hardPairCache={hardPairCompatibilityCache.Count}");
			work.Solved = DeterministicConstraintRepairSolver.TrySolve(work.Variables, work.Domains, PrepareEvaluationContext,
				EvaluateHardDistributionCandidate, HardDistributionFeasible,
				(leftVariable, leftCandidate, rightVariable, rightCandidate) => EvaluateHardConstraintPairCached(leftVariable, leftCandidate, rightVariable, rightCandidate, masks, candidateIds, hardPairCompatibilityCache),
				assigned => AcceptLayerHardSafety(assigned, masks, work.Variables), PositiveHash($"layer-hard-safety:{work.Layer}:{Main.ActiveWorldFileData?.Seed}"), ConstraintHardSafetyFallbackRestartLimit, ConstraintRepairStepsPerRestart, ConstraintRepairPairEvaluationLimit, ConstraintRepairCandidateEvaluationLimit, work.RepairBudget, out work.Solution, out work.RepairStats);
			if (work.Solution.Count == work.Variables.Count)
				work.CompleteSeed = new List<KeyValuePair<string, RankedLayoutCandidate>>(work.Solution);
			work.Method = "layer-hard-safety";
			layerTraces.Add($"{work.Layer}:{work.Method}:solved={work.Solved}:variables={work.Variables.Count}:values={work.DomainValues}:steps={work.RepairStats.Steps}:bestRepair={work.RepairStats.BestConflictCount}:timeBudgetExhausted={work.RepairStats.TimeBudgetExhausted}");
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=layer-hard-safety-end|layer={work.Layer}|solved={work.Solved}|elapsedMs={Environment.TickCount64 - started}|steps={work.RepairStats.Steps}|bestRepair={work.RepairStats.BestConflictCount}|timeBudgetExhausted={work.RepairStats.TimeBudgetExhausted}|hardPairCache={hardPairCompatibilityCache.Count}");
		}
	}
}
