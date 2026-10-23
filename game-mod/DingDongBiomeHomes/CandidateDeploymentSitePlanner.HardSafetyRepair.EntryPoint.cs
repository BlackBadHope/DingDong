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
	private static bool TryRepairHardSafetyBestAssignment(IReadOnlyList<KeyValuePair<string, RankedLayoutCandidate>> seedAssignment, out List<KeyValuePair<string, RankedLayoutCandidate>> repairedSolution, out string repairTrace, IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, long deadlineTick, ref string safetyFailure, ref string safetyReport)
	{
		HardSafetyRepairSession session = new(profiles, masks, unrestrictedPools, variables, profileByNpc, candidateIds, hardPairCompatibilityCache, deadlineTick, safetyFailure, safetyReport);
		bool solved = session.TryRepair(seedAssignment, out repairedSolution, out repairTrace);
		safetyFailure = session.SafetyFailure;
		safetyReport = session.SafetyReport;
		return solved;
	}
}
