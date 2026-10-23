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
	private static bool TryRepairCrossLayerMerge(out int moves, out int remainingConflicts, IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<string, Dictionary<string, int>> rejectionReasons, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache pairCompatibilityCache, ref string completeFailure, Dictionary<string, RankedLayoutCandidate> merged)
	{
		CrossLayerRepairSession session = new(profiles, presets, masks, unrestrictedPools, variables, profileByNpc, rejectionReasons, candidateIds, pairCompatibilityCache, merged, completeFailure);
		bool solved = session.TryRepair(out moves, out remainingConflicts);
		completeFailure = session.CompleteFailure;
		return solved;
	}
}
