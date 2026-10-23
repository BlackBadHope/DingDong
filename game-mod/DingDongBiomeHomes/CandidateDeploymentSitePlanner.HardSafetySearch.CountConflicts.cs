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
	private static int CountConflicts(out List<(string Left, string Right)> conflicts, Dictionary<string, AuthoredSceneMutationMask> masks, List<string> variables, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> working)
	{
		conflicts = new List<(string Left, string Right)>();
		for (int leftIndex = 0; leftIndex < variables.Count; leftIndex++)
		{
			string left = variables[leftIndex];
			for (int rightIndex = leftIndex + 1; rightIndex < variables.Count; rightIndex++)
			{
				string right = variables[rightIndex];
				if (!EvaluateHardConstraintPairCached(left, working[left], right, working[right], masks, candidateIds, hardPairCompatibilityCache).Compatible)
					conflicts.Add((left, right));
			}
		}

		return conflicts.Count;
	}
}
