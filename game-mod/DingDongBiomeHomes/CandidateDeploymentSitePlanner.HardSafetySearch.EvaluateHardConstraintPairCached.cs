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
	private static ConstraintPairCompatibilityEvaluation EvaluateHardConstraintPairCached(string leftVariable, RankedLayoutCandidate leftCandidate, string rightVariable, RankedLayoutCandidate rightCandidate, Dictionary<string, AuthoredSceneMutationMask> masks, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache)
	{
		int leftId = candidateIds[leftCandidate];
		int rightId = candidateIds[rightCandidate];
		if (hardPairCompatibilityCache.TryGet(leftId, rightId, out bool cached))
			return new ConstraintPairCompatibilityEvaluation(cached, true);
		CandidateDeploymentSite leftSite = leftCandidate.Site;
		CandidateDeploymentSite rightSite = rightCandidate.Site;
		AuthoredSceneMutationMask leftMask = masks[leftVariable];
		AuthoredSceneMutationMask rightMask = masks[rightVariable];
		AuthoredResidencePlacementContract leftContract = AuthoredResidencePlacementManifest.Required(leftVariable);
		AuthoredResidencePlacementContract rightContract = AuthoredResidencePlacementManifest.Required(rightVariable);
		bool sharedPortSettlement = AuthoredResidenceLayoutPolicy.CanSharePortSettlement(leftContract, rightContract);
		bool visualFocusCompatible = sharedPortSettlement || !IntersectsPlannedVisualFocus(leftSite.AssetArea, leftMask, leftContract.BroadLayer, rightSite, rightMask, rightContract.BroadLayer);
		bool protectedAreasDisjoint = !ResidenceEnvelopesOverlap(leftSite, rightSite);
		bool exactMutationOverlap = leftMask.IntersectsTranslated(leftSite.AssetArea.Location, rightMask, rightSite.AssetArea.Location);
		bool hardPairValid = AuthoredResidenceLayoutPolicy.TryValidateHardPair(leftContract, leftMask, leftSite, rightContract, rightMask, rightSite, out _);
		// Only explicitly shareable port residences may relax the visual gap.
		// Protected envelopes, exact authored mutations, owner constraints and
		// every non-port visual focus remain fail-closed.
		bool compatible = visualFocusCompatible && protectedAreasDisjoint && !exactMutationOverlap && hardPairValid;
		hardPairCompatibilityCache.Store(leftId, rightId, compatible);
		return new ConstraintPairCompatibilityEvaluation(compatible, false);
	}
}
