using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Candidate-envelope search shared by the independent 3F preview gate and
/// normal world generation. Independent planning intentionally ignores other
/// DingDong scenes. Simultaneous planning broad-phases with every accepted
/// protected footprint, keeps each primary authored residence focus visually
/// separate, resolves sparse-envelope overlaps against the exact mutation
/// masks, and rejects persistent-object collisions before any tile mutation.
/// </summary>
public static partial class CandidateDeploymentSitePlanner
{
	private static bool TrySolveHardSafetyFallback(IReadOnlyList<KeyValuePair<string, RankedLayoutCandidate>> componentSeed, out List<KeyValuePair<string, RankedLayoutCandidate>> fallbackSolution, out string fallbackTrace, Mod mod, IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache)
	{
		string safetyFailure = "not-checked";
		string safetyReport = "none";
		long started = Environment.TickCount64;
		long deadlineTick = started + ConstraintHardSafetyFallbackTimeLimitMilliseconds;
		int domainValues = 0;
		foreach (IReadOnlyList<RankedLayoutCandidate> domain in unrestrictedPools.Values)
			domainValues += domain.Count;
		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=hard-safety-fallback-start|variables={variables.Count}|values={domainValues}|timeLimitMs={ConstraintHardSafetyFallbackTimeLimitMilliseconds}|hardPairCache={hardPairCompatibilityCache.Count}");
		string componentTargetedTrace = "attempted=False";
		if (componentSeed != null && componentSeed.Count == variables.Count && TryRepairHardSafetyBestAssignment(componentSeed, out List<KeyValuePair<string, RankedLayoutCandidate>> componentTargetedSolution, out componentTargetedTrace, profiles, masks, unrestrictedPools, variables, profileByNpc, candidateIds, hardPairCompatibilityCache, deadlineTick, ref safetyFailure, ref safetyReport))
		{
			fallbackSolution = componentTargetedSolution;
			fallbackTrace = $"solved=True|elapsedMs={Environment.TickCount64 - started}|source=component-targeted|componentTargeted={componentTargetedTrace}|hardPairCache={hardPairCompatibilityCache.Count}|safety={safetyFailure}|report={safetyReport}";
			mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=hard-safety-fallback-end|{fallbackTrace}");
			return true;
		}

		int remainingMilliseconds = (int)Math.Max(1L, deadlineTick - Environment.TickCount64);
		bool solved = DeterministicConstraintRepairSolver.TrySolve(variables,
			unrestrictedPools, PrepareEvaluationContext,
			EvaluateHardDistributionCandidate, HardDistributionFeasible,
			(string leftVariable, RankedLayoutCandidate leftCandidate,
				string rightVariable, RankedLayoutCandidate rightCandidate) =>
				EvaluateHardConstraintPairCached(leftVariable, leftCandidate,
					rightVariable, rightCandidate, masks, candidateIds,
					hardPairCompatibilityCache),
			assigned => AcceptHardSafetyComplete(assigned, profiles, masks,
				variables, ref safetyFailure, ref safetyReport),
			PositiveHash($"hard-safety-fallback:{PlanningSeedKey}"),
			ConstraintHardSafetyFallbackRestartLimit,
			ConstraintRepairStepsPerRestart, ConstraintRepairPairEvaluationLimit,
			ConstraintRepairCandidateEvaluationLimit,
			remainingMilliseconds,
			out fallbackSolution, out ConstraintRepairStats<string> stats);
		string targetedTrace = "attempted=False";
		if (!solved && fallbackSolution.Count == variables.Count && Environment.TickCount64 < deadlineTick)
		{
			solved = TryRepairHardSafetyBestAssignment(fallbackSolution, out List<KeyValuePair<string, RankedLayoutCandidate>> targetedSolution, out targetedTrace, profiles, masks, unrestrictedPools, variables, profileByNpc, candidateIds, hardPairCompatibilityCache, deadlineTick, ref safetyFailure, ref safetyReport);
			if (solved)
				fallbackSolution = targetedSolution;
		}

		long elapsed = Environment.TickCount64 - started;
		fallbackTrace = $"solved={solved}|elapsedMs={elapsed}|restarts={stats.Restarts}/{ConstraintHardSafetyFallbackRestartLimit}|steps={stats.Steps}|bestConflicts={stats.BestConflictCount}|pairLookups={stats.PairLookups}|pairEvaluations={stats.PairEvaluations}|candidateEvaluations={stats.CandidateEvaluations}|timeBudgetExhausted={stats.TimeBudgetExhausted}|hardPairCache={hardPairCompatibilityCache.Count}|componentTargeted={componentTargetedTrace}|targeted={targetedTrace}|safety={safetyFailure}|report={safetyReport}";
		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=hard-safety-fallback-end|{fallbackTrace}");
		return solved;
	}
}
