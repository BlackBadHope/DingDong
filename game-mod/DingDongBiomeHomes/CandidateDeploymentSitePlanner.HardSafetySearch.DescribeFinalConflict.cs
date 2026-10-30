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
	private static string DescribeFinalConflict((string Left, string Right) pair, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> working)
	{
		RankedLayoutCandidate leftCandidate = working[pair.Left];
		RankedLayoutCandidate rightCandidate = working[pair.Right];
		CandidateDeploymentSite leftSite = leftCandidate.Site;
		CandidateDeploymentSite rightSite = rightCandidate.Site;
		AuthoredSceneMutationMask leftMask = masks[pair.Left];
		AuthoredSceneMutationMask rightMask = masks[pair.Right];
		AuthoredResidencePlacementContract leftContract = AuthoredResidencePlacementManifest.Required(pair.Left);
		AuthoredResidencePlacementContract rightContract = AuthoredResidencePlacementManifest.Required(pair.Right);
		string reason;
		if (ResidenceEnvelopesOverlap(leftSite, rightSite))
			reason = "protected-envelope";
		else if (leftMask.IntersectsTranslated(leftSite.AssetArea.Location, rightMask, rightSite.AssetArea.Location))
			reason = "exact-mutation-mask";
		else if (!AuthoredResidenceLayoutPolicy.TryValidateHardPair(leftContract, leftMask, leftSite, rightContract, rightMask, rightSite, out string pairReason))
			reason = pairReason;
		else
			reason = "unknown";
		bool hasPairSupport = false;
		foreach (RankedLayoutCandidate leftOption in unrestrictedPools[pair.Left])
		{
			foreach (RankedLayoutCandidate rightOption in unrestrictedPools[pair.Right])
			{
				if (!EvaluateHardConstraintPairCached(pair.Left, leftOption, pair.Right, rightOption, masks, candidateIds, hardPairCompatibilityCache).Compatible)
					continue;
				hasPairSupport = true;
				break;
			}

			if (hasPairSupport)
				break;
		}

		return $"{pair.Left}>{pair.Right}:{reason}:pairSupport={hasPairSupport}";
	}
}
