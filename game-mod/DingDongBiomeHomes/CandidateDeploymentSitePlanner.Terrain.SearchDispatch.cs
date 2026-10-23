using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public static partial class CandidateDeploymentSitePlanner
{
	private static bool TryPlanSurfaceOrSky(AuthoredDeploymentProfile profile, AuthoredPreset preset, AuthoredSceneMutationMask mutationMask, List<Rectangle> reserved, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, SurfaceColumnIndex surfaceIndex, int layoutSalt, bool allowPersistentAdvisory, out CandidateDeploymentSite site, out string failure)
	{
		var search = new SurfacePlanningSearch(profile, preset, mutationMask, reserved, plannedSites, allMasks, surfaceIndex, layoutSalt, allowPersistentAdvisory);
		try { return search.TryFind(out site, out failure); }
		finally { search.CaptureCandidateDomainDiagnostics(); }
	}

	private static bool TryPlanCavern(AuthoredDeploymentProfile profile, AuthoredPreset preset, AuthoredSceneMutationMask mutationMask, List<Rectangle> reserved, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, SurfaceColumnIndex surfaceIndex, int layoutSalt, bool allowPersistentAdvisory, out CandidateDeploymentSite site, out string failure)
	{
		return new CavernPlanningSearch(profile, preset, mutationMask, reserved, plannedSites, allMasks, surfaceIndex, layoutSalt, allowPersistentAdvisory).TryFind(out site, out failure);
	}
}
