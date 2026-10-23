using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		private int MaximumAllowedRelief()
		{
			int maximum = MaximumSurfaceRelief(contract.ScaleClass);
			if (profile.TargetBiome == HomeBiome.Ocean && profile.TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope)
				maximum = System.Math.Max(maximum, CoastalTerrainEnvelopeMaximumRelief);
			if (contract.Kind == ResidenceKind.Treehouse && contract.PreferredBiome == HomeBiome.Jungle && profile.TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope)
				maximum = System.Math.Max(maximum, JungleCanopyTerrainEnvelopeMaximumRelief);
			if (contract.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega && contract.PreferredBiome == HomeBiome.Snow && profile.TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope)
				maximum = System.Math.Max(maximum, SnowLandmarkTerrainEnvelopeMaximumRelief);
			return maximum;
		}

		private bool TryValidateFootprints(Rectangle area, Rectangle visual, out Rectangle result)
		{
			Rectangle protectedArea = mutationMask.ProtectedFootprint(area.Location, profile.ProtectedPadding);
			result = protectedArea;
			if (!InWorld(protectedArea))
			{
				boundsRejected++;
				return false;
			}
			if (profile.TargetAnchor == DeploymentTargetAnchor.SkyClearanceEnvelope && area.Bottom > (int)Main.worldSurface - 24)
				return RejectTerrain("sky-bottom", $"bottom={area.Bottom},worldSurface={Main.worldSurface}");
			if (IntersectsPlannedVisualFocus(area, mutationMask, plannedSites, allMasks, contract.BroadLayer) ||
				IntersectsReserved(protectedArea, reserved) && IntersectsPlannedMutations(area, mutationMask, plannedSites, allMasks))
			{
				reservedRejected++;
				return false;
			}

			Rectangle criticalArea = profile.TargetBiome == HomeBiome.Ocean || profile.TargetAnchor == DeploymentTargetAnchor.CoastalWaterlineEnvelope ? visual : protectedArea;
			if (ContainsWorldCriticalAnchor(criticalArea, area, profile.BlendMargin, mutationMask))
			{
				protectedStructureRejected++;
				return false;
			}
			if (profile.TargetAnchor == DeploymentTargetAnchor.SkyClearanceEnvelope && SolidSampleRatio(area) > 0.18)
				return RejectTerrain("sky-solid", $"x={area.X},y={area.Y}");
			return true;
		}
	}
}
