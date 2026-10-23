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
	private static bool AcceptComplete(IReadOnlyDictionary<string, RankedLayoutCandidate> assigned, IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredSceneMutationMask> masks, ref string completeFailure)
	{
		List<CandidateDeploymentSite> planned = SitesInManifestOrder(profiles, assigned);
		if (!PersistentChestRelocator.TryPlanAll(planned, out _, out string relocationFailure))
		{
			completeFailure = $"relocation={relocationFailure}";
			return false;
		}

		if (!AuthoredResidenceLayoutPolicy.TryAccept(planned, masks, out string distributionFailure,
			out string distributionReport, enforceOceanBalance: false))
		{
			// Coverage across macro-sectors is a visual variety preference, not a
			// collision or world-safety rule. A real 67-mod world produced many
			// complete, collision-free 61/61 layouts with exactly one coverage gap;
			// rejecting all of them made world creation fail after the bounded solver
			// timed out. Keep every hard placement rule, but admit that one aesthetic
			// gap as a deterministic final fallback.
			bool aestheticCoverageGap = string.Equals(distributionFailure, "layer-coverage-violations=1", StringComparison.Ordinal);
			bool aestheticSkyBandGap = distributionFailure.StartsWith("sky-altitude-distribution=", StringComparison.Ordinal);
			// Technical admission already allows megaCell via hard-safety
			// (enforceAestheticDistribution=false). Owner v10u found a complete
			// 61/61 with only mega-sector-violations=1, then threw it away and
			// timed out re-solving. Admit that one aesthetic mega-bin gap.
			bool aestheticMegaCell = string.Equals(distributionFailure, "mega-sector-violations=1", StringComparison.Ordinal);
			// Owner v10v: layers SAT, megaCell=0, sky 3 bands, merge leftover
			// visual-spacing-violations=1 (Musician>Squirrel). 10p already
			// accepted that pair at 248 tiles; the leftover is HardVisualGap
			// padding, not overlap (isolatedEnvelope=0) or same-sector
			// landmark stacking. Hard-safety timed out re-solving and
			// discarded the only non-clustered 61/61. Admit that one miss.
			bool aestheticVisualSpacing = string.Equals(distributionFailure, "visual-spacing-violations=1", StringComparison.Ordinal);
			if (aestheticMegaCell || aestheticVisualSpacing)
			{
				if (AuthoredResidenceLayoutPolicy.TryAccept(planned, masks, out distributionFailure, out distributionReport, requireFullLayerPopulation: true, maximumLayerCoverageViolations: int.MaxValue, minimumDistinctSkyBands: 1, enforceAestheticDistribution: false, maximumVisualSpacingViolations: aestheticVisualSpacing ? 1 : 0, enforceOceanBalance: false))
				{
					completeFailure = aestheticVisualSpacing ? $"accepted-aesthetic-visual-spacing-fallback:{distributionReport}" : $"accepted-aesthetic-mega-cell-fallback:{distributionReport}";
					return true;
				}

				completeFailure = $"distribution={distributionFailure}:{distributionReport}";
				return false;
			}

			if ((!aestheticCoverageGap && !aestheticSkyBandGap) || !AuthoredResidenceLayoutPolicy.TryAccept(planned, masks, out distributionFailure, out distributionReport, requireFullLayerPopulation: true, maximumLayerCoverageViolations: 1, minimumDistinctSkyBands: 2, enforceOceanBalance: false))
			{
				completeFailure = $"distribution={distributionFailure}:{distributionReport}";
				return false;
			}

			completeFailure = $"accepted-aesthetic-distribution-fallback:{distributionReport}";
			return true;
		}

		completeFailure = "none";
		return true;
	}
}
