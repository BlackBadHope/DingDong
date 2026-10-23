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
	private static List<RankedLayoutCandidate> ViableAgainstFixed(string variable, string otherMovingVariable, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> working)
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

				if (!EvaluateHardConstraintPairCached(variable, candidate, fixedVariable, working[fixedVariable], masks, candidateIds, hardPairCompatibilityCache).Compatible)
				{
					compatible = false;
					break;
				}
			}

			if (compatible)
				viable.Add(candidate);
		}

		return viable;
	}
}
