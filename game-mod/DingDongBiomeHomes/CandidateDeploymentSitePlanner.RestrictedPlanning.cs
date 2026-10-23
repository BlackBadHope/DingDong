using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static bool TrySolveRestrictedAfterCapacity(
		Mod mod,
		IReadOnlyList<AuthoredDeploymentProfile> profiles,
		Dictionary<string, AuthoredPreset> presets,
		Dictionary<string, AuthoredSceneMutationMask> masks,
		IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools,
		List<string> variables,
		Dictionary<string, AuthoredDeploymentProfile> profileByNpc,
		Dictionary<string, Dictionary<string, int>> rejectionReasons,
		Dictionary<RankedLayoutCandidate, int> candidateIds,
		CandidatePairCompatibilityCache pairCompatibilityCache,
		CandidatePairCompatibilityCache hardPairCompatibilityCache,
		string poolSummary,
		string unrestrictedTrace,
		string capacitySummary,
		ref string completeFailure,
		out List<CandidateDeploymentSite> sites,
		out string failure)
	{
		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=restricted-layer-components-start|layers=4|variables={variables.Count}");
		if (TrySolveIndependentBroadLayers(
			out List<KeyValuePair<string, RankedLayoutCandidate>> layerSolution,
			out string layerTrace, mod, profiles, presets, masks, pools, variables,
			profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache,
			hardPairCompatibilityCache, ref completeFailure))
		{
			LastSolverTrace = $"phase=restricted-layer-components|{poolSummary}|capacity={capacitySummary}|{layerTrace}|pairCache={pairCompatibilityCache.Count}";
			return CompleteSolution(layerSolution, out sites, out failure);
		}

		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=restricted-layer-components-fallback|trace={layerTrace}|pairCache={pairCompatibilityCache.Count}");
		if (TrySolveHardSafetyFallback(layerSolution,
			out List<KeyValuePair<string, RankedLayoutCandidate>> hardSolution,
			out string hardTrace, mod, profiles, masks, pools, variables,
			profileByNpc, candidateIds, hardPairCompatibilityCache))
		{
			LastSolverTrace = $"phase=restricted-hard-safety-fallback|{poolSummary}|capacity={capacitySummary}|componentFailure={layerTrace}|{hardTrace}";
			return CompleteSolution(hardSolution, out sites, out failure);
		}

		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=restricted-hard-safety-fallback-miss|trace={hardTrace}");
		LastSolverTrace = $"phase=hard-safety-unavailable|{poolSummary}|unrestricted={unrestrictedTrace}|capacity={capacitySummary}|componentFailure={layerTrace}|hardFallback={hardTrace}|legacySlowSolver=retired";
		sites = new List<CandidateDeploymentSite>();
		failure = $"safe-layout-not-found:{LastSolverTrace}";
		return false;
	}
}
