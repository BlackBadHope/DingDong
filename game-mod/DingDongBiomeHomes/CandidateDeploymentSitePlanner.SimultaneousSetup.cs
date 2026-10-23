using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
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
	private static void BuildPlanningIndexes(IReadOnlyList<AuthoredDeploymentProfile> profiles, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools, out List<string> variables, out Dictionary<string, AuthoredDeploymentProfile> profileByNpc, out Dictionary<RankedLayoutCandidate, int> candidateIds)
	{
		variables = new List<string>(profiles.Count);
		profileByNpc = new Dictionary<string, AuthoredDeploymentProfile>(StringComparer.Ordinal);
		foreach (AuthoredDeploymentProfile profile in profiles)
		{
			variables.Add(profile.NpcKey);
			profileByNpc.Add(profile.NpcKey, profile);
		}

		candidateIds = new Dictionary<RankedLayoutCandidate, int>();
		int nextCandidateId = 0;
		foreach (IReadOnlyList<RankedLayoutCandidate> pool in pools.Values)
		{
			foreach (RankedLayoutCandidate candidate in pool)
				candidateIds.Add(candidate, nextCandidateId++);
		}
	}

	private static bool CompleteSolution(IReadOnlyList<KeyValuePair<string, RankedLayoutCandidate>> solution, out List<CandidateDeploymentSite> sites, out string failure)
	{
		sites = new List<CandidateDeploymentSite>(solution.Count);
		foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in solution)
			sites.Add(assignment.Value.Site);
		sites.Sort((left, right) => ManifestOrder(left.NpcKey).CompareTo(ManifestOrder(right.NpcKey)));
		failure = string.Empty;
		return true;
	}

	private static bool TryValidateSolverStack(out string failure)
	{
		if (!CandidatePairCompatibilityCache.TrySelfTest(out string cacheFailure))
			return FailSolverSelfTest("pair-cache", cacheFailure, out failure);
		if (!DeterministicConstraintSolver.TrySelfTest(out string solverFailure))
			return FailSolverSelfTest("solver", solverFailure, out failure);
		if (!DeterministicConstraintRepairSolver.TrySelfTest(out string repairFailure))
			return FailSolverSelfTest("repair", repairFailure, out failure);
		if (!DeterministicConstraintBeamSolver.TrySelfTest(out string beamFailure))
			return FailSolverSelfTest("beam", beamFailure, out failure);
		if (!DeterministicArcConsistencyPruner.TrySelfTest(out string arcFailure))
			return FailSolverSelfTest("arc", arcFailure, out failure);
		failure = string.Empty;
		return true;
	}

	private static bool FailSolverSelfTest(string solver, string reason, out string failure)
	{
		LastSolverTrace = $"phase={solver}-self-test|verdict=FAIL|failure={reason}";
		failure = $"{solver}-self-test:{reason}";
		return false;
	}

	private static bool TryLoadPlanningAssets(Mod mod, IReadOnlyList<AuthoredDeploymentProfile> profiles, out Dictionary<string, AuthoredPreset> presets, out Dictionary<string, AuthoredSceneMutationMask> masks, out string failure)
	{
		presets = new Dictionary<string, AuthoredPreset>(StringComparer.Ordinal);
		masks = new Dictionary<string, AuthoredSceneMutationMask>(StringComparer.Ordinal);
		foreach (AuthoredDeploymentProfile profile in profiles)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out AuthoredPreset preset) || !string.Equals(preset.Id, profile.PresetId, StringComparison.Ordinal))
			{
				failure = $"candidate-not-found:{profile.NpcKey}/{profile.PresetId}";
				return false;
			}

			AuthoredSceneMutationMask mask = DingDongAuthoredStructureLoader.InspectMutationMask(mod, preset, profile);
			if (mask.MutationCellCount <= 0)
			{
				failure = $"mutation-mask-empty:{profile.NpcKey}/{profile.PresetId}";
				return false;
			}

			presets.Add(profile.NpcKey, preset);
			masks.Add(profile.NpcKey, mask);
		}

		failure = string.Empty;
		return true;
	}
}
