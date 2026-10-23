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
	private static int CountCrossLayerConflicts(Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<string, Dictionary<string, int>> rejectionReasons, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache pairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> merged)
	{
		int conflicts = 0;
		for (int leftIndex = 0; leftIndex < variables.Count; leftIndex++)
		{
			string leftVariable = variables[leftIndex];
			BroadLayoutLayer leftLayer = AuthoredResidencePlacementManifest.Required(leftVariable).BroadLayer;
			for (int rightIndex = leftIndex + 1; rightIndex < variables.Count; rightIndex++)
			{
				string rightVariable = variables[rightIndex];
				if (AuthoredResidencePlacementManifest.Required(rightVariable).BroadLayer == leftLayer)
					continue;
				if (!EvaluateConstraintPairCached(leftVariable, merged[leftVariable], rightVariable, merged[rightVariable], presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache).Compatible)
					conflicts++;
			}
		}

		return conflicts;
	}

	private static int CrossConflictsFor(string variable, RankedLayoutCandidate candidate, Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<string, Dictionary<string, int>> rejectionReasons, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache pairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> merged)
	{
		BroadLayoutLayer layer = AuthoredResidencePlacementManifest.Required(variable).BroadLayer;
		int conflicts = 0;
		foreach (string other in variables)
		{
			if (string.Equals(other, variable, StringComparison.Ordinal) || AuthoredResidencePlacementManifest.Required(other).BroadLayer == layer)
			{
				continue;
			}

			if (!EvaluateConstraintPairCached(variable, candidate, other, merged[other], presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache).Compatible)
				conflicts++;
		}

		return conflicts;
	}

	private static ConstraintPairCompatibilityEvaluation EvaluateConstraintPairCached(string leftVariable, RankedLayoutCandidate leftCandidate, string rightVariable, RankedLayoutCandidate rightCandidate, Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<string, Dictionary<string, int>> rejectionReasons, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache pairCompatibilityCache)
	{
		int leftId = candidateIds[leftCandidate];
		int rightId = candidateIds[rightCandidate];
		if (pairCompatibilityCache.TryGet(leftId, rightId, out bool cached))
			return new ConstraintPairCompatibilityEvaluation(cached, true);
		bool compatible = EvaluateConstraintPair(profileByNpc[leftVariable], presets[leftVariable], masks[leftVariable], leftCandidate, profileByNpc[rightVariable], presets[rightVariable], masks[rightVariable], rightCandidate, masks, rejectionReasons);
		pairCompatibilityCache.Store(leftId, rightId, compatible);
		return new ConstraintPairCompatibilityEvaluation(compatible, false);
	}
}
