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
	private static bool TryRepairConflictGroup(IReadOnlyList<string> conflictVariables, int baselineConflicts, out int attemptedVariables, out int attemptedDomains, out int visitedNodes, out long evaluatedPairs, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> working)
	{
		return new ConflictGroupRepairSearch(conflictVariables, baselineConflicts, masks, unrestrictedPools, variables, profileByNpc, candidateIds, hardPairCompatibilityCache, working).TryRepair(out attemptedVariables, out attemptedDomains, out visitedNodes, out evaluatedPairs);
	}
}
