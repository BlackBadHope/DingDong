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
	private static List<RankedLayoutCandidate> ViableAcrossFixedLayers(string variable, string otherMovingVariable, List<CandidateDeploymentSite> fixedSites, Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<string, Dictionary<string, int>> rejectionReasons, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache pairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> merged)
	{
		List<RankedLayoutCandidate> viable = new();
		foreach (RankedLayoutCandidate candidate in unrestrictedPools[variable])
		{
			bool compatible = true;
			foreach (string fixedVariable in variables)
			{
				if (string.Equals(fixedVariable, variable, StringComparison.Ordinal) || string.Equals(fixedVariable, otherMovingVariable, StringComparison.Ordinal))
				{
					continue;
				}

				if (!EvaluateConstraintPairCached(variable, candidate, fixedVariable, merged[fixedVariable], presets, masks, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache).Compatible)
				{
					compatible = false;
					break;
				}
			}

			if (!compatible || !EvaluateForward(variable, candidate, fixedSites, profileByNpc, rejectionReasons) || !EvaluateCandidate(variable, candidate, fixedSites, presets, masks, profileByNpc, rejectionReasons).Feasible)
			{
				continue;
			}

			viable.Add(candidate);
		}

		return viable;
	}
}
