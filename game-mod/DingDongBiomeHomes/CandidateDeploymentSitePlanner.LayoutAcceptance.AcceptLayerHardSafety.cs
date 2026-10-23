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
	private static bool AcceptLayerHardSafety(IReadOnlyDictionary<string, RankedLayoutCandidate> assigned, Dictionary<string, AuthoredSceneMutationMask> masks, List<string> layerVariables)
	{
		if (assigned.Count != layerVariables.Count)
			return false;
		List<CandidateDeploymentSite> planned = new(layerVariables.Count);
		foreach (string variable in layerVariables)
			planned.Add(assigned[variable].Site);
		return AuthoredResidenceLayoutPolicy.TryAccept(planned, masks, out _, out _,
			requireFullLayerPopulation: false, maximumLayerCoverageViolations: 0,
			minimumDistinctSkyBands: 2, enforceAestheticDistribution: true,
			maximumVisualSpacingViolations: int.MaxValue,
			allowDisjointFocusOverlap: true, enforceOceanBalance: false);
	}
}
