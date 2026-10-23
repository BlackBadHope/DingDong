using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public static partial class CandidateDeploymentSitePlanner
{
	public static bool TryPlanSimultaneous(Mod mod, IReadOnlyList<AuthoredDeploymentProfile> profiles, out List<CandidateDeploymentSite> sites, out string failure)
	{
		if (!TryValidateSolverStack(out failure) || !TryLoadPlanningAssets(mod, profiles, out Dictionary<string, AuthoredPreset> presets, out Dictionary<string, AuthoredSceneMutationMask> masks, out failure))
		{
			sites = new List<CandidateDeploymentSite>();
			return false;
		}

		SurfaceColumnIndex surfaceIndex = SurfaceColumnIndex.Build();
		if (!TryBuildConstraintCandidatePools(mod, profiles, presets, masks, surfaceIndex, out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools, out string poolSummary, out string poolFailure))
		{
			LastSolverTrace = $"phase=candidate-pools|{poolSummary}|failure={poolFailure}";
			sites = new List<CandidateDeploymentSite>();
			failure = poolFailure;
			return false;
		}

		mod.Logger.Info($"DINGDONG_PLANNER_PHASE|phase=candidate-pools-complete|{poolSummary}");
		CaptureDiagnosticPools(mod, pools);
		IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools = pools;
		BuildPlanningIndexes(profiles, pools, out List<string> variables, out Dictionary<string, AuthoredDeploymentProfile> profileByNpc, out Dictionary<RankedLayoutCandidate, int> candidateIds);
		Dictionary<string, Dictionary<string, int>> rejectionReasons = new(StringComparer.Ordinal);
		CandidatePairCompatibilityCache pairCompatibilityCache = new(candidateIds.Count);
		CandidatePairCompatibilityCache hardPairCompatibilityCache = new(candidateIds.Count);
		mod.Logger.Info($"DINGDONG_PLANNER_PAIR_CACHE|candidates={candidateIds.Count}|pairs={pairCompatibilityCache.PairCapacity}|bytesPerCache={pairCompatibilityCache.AllocatedBytes}");
		string completeFailure = "none";
		if (TrySolveUnrestrictedFirst(mod, profiles, presets, masks, unrestrictedPools,
			variables, profileByNpc, rejectionReasons, candidateIds,
			pairCompatibilityCache, hardPairCompatibilityCache, ref completeFailure,
			out List<KeyValuePair<string, RankedLayoutCandidate>> unrestrictedSolution,
			out string unrestrictedTrace))
		{
			LastSolverTrace = $"phase=unrestricted-first|{poolSummary}|{unrestrictedTrace}";
			return CompleteSolution(unrestrictedSolution, out sites, out failure);
		}

		return TrySolveCapacityFallback(mod, profiles, presets, masks,
			unrestrictedPools, variables, profileByNpc, rejectionReasons,
			candidateIds, pairCompatibilityCache, hardPairCompatibilityCache,
			poolSummary, unrestrictedTrace, ref completeFailure, out sites, out failure);
	}
}
