using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static bool TrySolveCapacityFallback(
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
		ref string completeFailure,
		out List<CandidateDeploymentSite> sites,
		out string failure)
	{
		if (!TryBuildCapacityRestrictedPools(mod, profiles, variables, pools,
			masks, candidateIds, hardPairCompatibilityCache,
			out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> capacityPools,
			out List<KeyValuePair<string, RankedLayoutCandidate>> capacitySolution,
			out string capacitySummary, out string capacityFailure))
		{
			LastSolverTrace = $"phase=capacity-sectors|{poolSummary}|unrestricted={unrestrictedTrace}|{capacitySummary}|failure={capacityFailure}";
			sites = new List<CandidateDeploymentSite>();
			failure = $"capacity-layout-not-found:{LastSolverTrace}";
			return false;
		}

		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=capacity-sectors-admitted|{capacitySummary}");
		if (capacitySolution.Count == variables.Count)
		{
			LastSolverTrace = $"phase=capacity-integrated-exact|{poolSummary}|capacity={capacitySummary}";
			return CompleteSolution(capacitySolution, out sites, out failure);
		}

		return TrySolveRestrictedAfterCapacity(mod, profiles, presets, masks,
			capacityPools, variables, profileByNpc, rejectionReasons, candidateIds,
			pairCompatibilityCache, hardPairCompatibilityCache, poolSummary,
			unrestrictedTrace, capacitySummary, ref completeFailure, out sites, out failure);
	}
}
