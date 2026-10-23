using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static bool TrySolveUnrestrictedFirst(
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
		ref string completeFailure,
		out List<KeyValuePair<string, RankedLayoutCandidate>> solution,
		out string trace)
	{
		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=unrestricted-layer-components-start|layers=4|variables={variables.Count}");
		if (TrySolveIndependentBroadLayers(out solution, out string layerTrace,
			mod, profiles, presets, masks, pools, variables, profileByNpc,
			rejectionReasons, candidateIds, pairCompatibilityCache,
			hardPairCompatibilityCache, ref completeFailure))
		{
			trace = $"method=layer-components|{layerTrace}|pairCache={pairCompatibilityCache.Count}";
			return true;
		}

		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=unrestricted-hard-safety-start|trace={layerTrace}|hardPairCache={hardPairCompatibilityCache.Count}");
		if (TrySolveHardSafetyFallback(solution,
			out List<KeyValuePair<string, RankedLayoutCandidate>> repaired,
			out string hardTrace, mod, profiles, masks, pools, variables,
			profileByNpc, candidateIds, hardPairCompatibilityCache))
		{
			solution = repaired;
			trace = $"method=hard-safety-fallback|componentFailure={layerTrace}|{hardTrace}";
			return true;
		}

		trace = $"method=miss|componentFailure={layerTrace}|hardFallback={hardTrace}";
		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=unrestricted-first-miss|trace={trace}");
		return false;
	}
}
