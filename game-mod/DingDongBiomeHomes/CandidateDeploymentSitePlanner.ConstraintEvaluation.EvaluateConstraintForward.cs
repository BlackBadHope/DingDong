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
	private static bool EvaluateConstraintForward(AuthoredDeploymentProfile profile, RankedLayoutCandidate candidate, IReadOnlyList<CandidateDeploymentSite> planned, Dictionary<string, Dictionary<string, int>> rejectionReasons)
	{
		AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(profile.NpcKey);
		if (AuthoredResidenceLayoutPolicy.TryValidateAggregateCapacity(contract, candidate.Site, planned, out string rejectReason))
			return true;
		RejectConstraintCandidate(profile.NpcKey, NormalizeConstraintRejectReason(rejectReason), rejectionReasons);
		return false;
	}
}
