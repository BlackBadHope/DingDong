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
	private static bool AcceptHardSafetyComplete(IReadOnlyDictionary<string, RankedLayoutCandidate> assigned, IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredSceneMutationMask> masks, List<string> variables, ref string safetyFailure, ref string safetyReport)
	{
		if (assigned.Count != variables.Count)
		{
			safetyFailure = $"incomplete={assigned.Count}/{variables.Count}";
			return false;
		}

		List<CandidateDeploymentSite> planned = SitesInManifestOrder(profiles, assigned);
		if (!PersistentChestRelocator.TryPlanAll(planned, out _, out string relocationFailure))
		{
			safetyFailure = $"relocation={relocationFailure}";
			return false;
		}

		if (!AuthoredResidenceLayoutPolicy.TryAccept(planned, masks,
			out string layoutFailure, out safetyReport, enforceOceanBalance: false,
			enforceAestheticDistribution: false))
		{
			safetyFailure = $"hard-layout={layoutFailure}";
			return false;
		}

		// The strict search already tried visual distribution. This fallback must
		// retain hard safety, not repeat the aesthetic gate under another name.
		safetyReport = "aestheticDistribution=fallback;" + safetyReport;
		safetyFailure = "none";
		return true;
	}
}
