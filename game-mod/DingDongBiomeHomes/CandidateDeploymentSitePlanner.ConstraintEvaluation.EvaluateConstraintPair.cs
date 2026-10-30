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
	private static bool EvaluateConstraintPair(AuthoredDeploymentProfile leftProfile, AuthoredPreset leftPreset, AuthoredSceneMutationMask leftMask, RankedLayoutCandidate left, AuthoredDeploymentProfile rightProfile, AuthoredPreset rightPreset, AuthoredSceneMutationMask rightMask, RankedLayoutCandidate right, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, Dictionary<string, Dictionary<string, int>> rejectionReasons)
	{
		CandidateDeploymentSite leftSite = left.Site;
		CandidateDeploymentSite rightSite = right.Site;
		CandidateDeploymentSite[] rightPlanned =
		{
			rightSite
		};
		AuthoredResidencePlacementContract leftContract = AuthoredResidencePlacementManifest.Required(leftProfile.NpcKey);
		AuthoredResidencePlacementContract rightContract = AuthoredResidencePlacementManifest.Required(rightProfile.NpcKey);
		bool sharedPortSettlement = AuthoredResidenceLayoutPolicy.CanSharePortSettlement(leftContract, rightContract);
		if (!sharedPortSettlement && IntersectsPlannedVisualFocus(leftSite.AssetArea, leftMask, rightPlanned, allMasks, leftContract.BroadLayer))
		{
			RejectConstraintCandidate(leftProfile.NpcKey, "visual-focus", rejectionReasons);
			return false;
		}

		if (ResidenceEnvelopesOverlap(leftSite, rightSite))
		{
			RejectConstraintCandidate(leftProfile.NpcKey, "protected-envelope", rejectionReasons);
			return false;
		}

		if (leftMask.IntersectsTranslated(leftSite.AssetArea.Location, rightMask, rightSite.AssetArea.Location))
		{
			RejectConstraintCandidate(leftProfile.NpcKey, "mutation-overlap", rejectionReasons);
			return false;
		}

		if (!AuthoredResidenceLayoutPolicy.TryScore(leftContract, leftPreset, leftMask, leftSite, rightPlanned, allMasks, out _, out string leftReason))
		{
			RejectConstraintCandidate(leftProfile.NpcKey, NormalizeConstraintRejectReason(leftReason), rejectionReasons);
			return false;
		}

		CandidateDeploymentSite[] leftPlanned =
		{
			leftSite
		};
		if (!AuthoredResidenceLayoutPolicy.TryScore(rightContract, rightPreset, rightMask, rightSite, leftPlanned, allMasks, out _, out string rightReason))
		{
			RejectConstraintCandidate(rightProfile.NpcKey, NormalizeConstraintRejectReason(rightReason), rejectionReasons);
			return false;
		}

		return true;
	}
}
