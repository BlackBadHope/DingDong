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
	private static bool TryPlan(AuthoredDeploymentProfile profile, AuthoredPreset preset, AuthoredSceneMutationMask mutationMask, List<Rectangle> reserved, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, SurfaceColumnIndex surfaceIndex, int layoutSalt, bool allowPersistentAdvisory, out CandidateDeploymentSite site, out string failure)
	{
		return profile.TargetAnchor switch
		{
			DeploymentTargetAnchor.SurfaceGroundContact or DeploymentTargetAnchor.SurfaceTerrainEnvelope or DeploymentTargetAnchor.CoastalWaterlineEnvelope or DeploymentTargetAnchor.SkyClearanceEnvelope => TryPlanSurfaceOrSky(profile, preset, mutationMask, reserved, plannedSites, allMasks, surfaceIndex, layoutSalt, allowPersistentAdvisory, out site, out failure),
			DeploymentTargetAnchor.OrganicCavernEnvelope or DeploymentTargetAnchor.OrganicMushroomCavernEnvelope => TryPlanCavern(profile, preset, mutationMask, reserved, plannedSites, allMasks, surfaceIndex, layoutSalt, allowPersistentAdvisory, out site, out failure),
			DeploymentTargetAnchor.UnderworldAshLavaShelf => TryPlanUnderworld(profile, preset, mutationMask, reserved, plannedSites, allMasks, layoutSalt, allowPersistentAdvisory, out site, out failure),
			_ => Fail("unknown-target-anchor", out site, out failure)};
	}

	private sealed partial class SurfaceColumnIndex
	{
	}
}
