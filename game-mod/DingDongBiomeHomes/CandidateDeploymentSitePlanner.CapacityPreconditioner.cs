using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static bool TryBuildCapacityRestrictedPools(
		Mod mod,
		IReadOnlyList<AuthoredDeploymentProfile> profiles,
		IReadOnlyList<string> variables,
		IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools,
		Dictionary<string, AuthoredSceneMutationMask> masks,
		Dictionary<RankedLayoutCandidate, int> candidateIds,
		CandidatePairCompatibilityCache hardPairCompatibilityCache,
		out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> restrictedPools,
		out List<KeyValuePair<string, RankedLayoutCandidate>> admittedSolution,
		out string summary,
		out string failure)
	{
		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=capacity-sectors-start|variables={variables.Count}|hardPairCache={hardPairCompatibilityCache.Count}");
		int downstreamChecks = 0;
		int downstreamNodes = 0;
		long downstreamPairLookups = 0;
		int downstreamPairEvaluations = 0;
		int downstreamLeafBudgetExhaustions = 0;
		int downstreamMaxAssigned = 0;
		string downstreamLastFailedLayer = "none";
		bool downstreamBudgetExhausted = false;
		long downstreamStarted = Environment.TickCount64;
		List<KeyValuePair<string, RankedLayoutCandidate>> witness = new();
		List<string> validationVariables = new(variables);
		string safetyFailure = "not-checked";
		string safetyReport = "none";
		bool solved = TryRestrictPoolsByCapacitySectors(
			variables,
			pools,
			(leftVariable, leftCandidate, rightVariable, rightCandidate) =>
				EvaluateHardConstraintPairCached(leftVariable, leftCandidate,
					rightVariable, rightCandidate, masks, candidateIds,
					hardPairCompatibilityCache).Compatible,
			candidateDomains =>
			{
				if (Environment.TickCount64 - downstreamStarted >= ConstraintSectorDownstreamTotalTimeLimitMilliseconds)
					return new CapacityDomainAdmission(false, BudgetExhausted: true);
				downstreamChecks++;
				bool accepted = TryFindPairCompatibleAssignment(variables,
					candidateDomains, masks, (leftVariable, leftCandidate, rightVariable,
					rightCandidate) => EvaluateHardConstraintPairCached(leftVariable,
					leftCandidate, rightVariable, rightCandidate, masks, candidateIds,
					hardPairCompatibilityCache), assigned => AcceptHardSafetyComplete(
					assigned, profiles, masks, validationVariables,
					ref safetyFailure, ref safetyReport),
					out List<KeyValuePair<string, RankedLayoutCandidate>> candidateSolution,
					out CapacityWitnessStats stats);
				downstreamNodes += stats.NodesVisited;
				downstreamPairLookups += stats.PairLookups;
				downstreamPairEvaluations += stats.PairEvaluations;
				downstreamMaxAssigned = Math.Max(downstreamMaxAssigned, stats.MaxAssigned);
				downstreamLastFailedLayer = stats.FailedLayer;
				if (stats.LeafBudgetExhausted)
					downstreamLeafBudgetExhaustions++;
				if (accepted)
					witness = candidateSolution;
				bool gateBudgetExhausted =
					Environment.TickCount64 - downstreamStarted >= ConstraintSectorDownstreamTotalTimeLimitMilliseconds;
				downstreamBudgetExhausted |= gateBudgetExhausted;
				return new CapacityDomainAdmission(accepted, gateBudgetExhausted);
			},
			out restrictedPools,
			out summary,
			out failure);
		admittedSolution = witness;
		summary += $"|sectorDownstreamChecks={downstreamChecks}" +
			$"|sectorDownstreamNodes={downstreamNodes}" +
			$"|sectorDownstreamPairLookups={downstreamPairLookups}" +
			$"|sectorDownstreamPairEvaluations={downstreamPairEvaluations}" +
			$"|sectorDownstreamLeafBudgetExhaustions={downstreamLeafBudgetExhaustions}" +
			$"|sectorDownstreamMaxAssigned={downstreamMaxAssigned}" +
			$"|sectorDownstreamLastFailedLayer={downstreamLastFailedLayer}" +
			$"|sectorDownstreamBudgetExhausted={downstreamBudgetExhausted}" +
			$"|sectorDownstreamElapsedMs={Environment.TickCount64 - downstreamStarted}" +
			$"|sectorDownstreamWitness={admittedSolution.Count}/{variables.Count}" +
			$"|sectorDownstreamSafety={safetyFailure}";
		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=capacity-sectors-end|solved={solved}|{summary}|failure={(string.IsNullOrEmpty(failure) ? "none" : failure)}|hardPairCache={hardPairCompatibilityCache.Count}");
		return solved;
	}
}
