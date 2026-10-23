using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		private bool TryEvaluateBand(int left, int rangeMin, int rangeMax, int centerX, int floorY, HomeBiome matchedBiome, string fallback, ResidenceAltitudeBand band, out CandidateDeploymentSite site)
		{
			Rectangle area = AreaAt(profile, preset, mutationMask, left, floorY, layoutSalt, band);
			Rectangle visual = mutationMask.VisualFootprint(area.Location, 0);
			if (!TryValidateTerrain(area, visual, floorY, matchedBiome, out int surfaceRelief) ||
				!TryValidateFootprints(area, visual, out Rectangle protectedArea) ||
				!HasCompatibleExteriorNeighbors(area, matchedBiome))
			{
				site = null;
				return false;
			}

			int persistentOverlaps = CountPersistentObjects(protectedArea, area, profile.BlendMargin, mutationMask, out IReadOnlyList<PersistentObjectCollision> collisions);
			CandidateDeploymentSite candidate = new(profile.NpcKey, profile.PresetId, matchedBiome, area, protectedArea, new Point(centerX, floorY), fallback, collisions, contract.BroadLayer == BroadLayoutLayer.Sky ? band : ResidenceAltitudeBand.Surface, surfaceRelief);
			return TryRankCandidate(candidate, persistentOverlaps, left, rangeMin, rangeMax, out site);
		}

		private bool TryValidateTerrain(Rectangle area, Rectangle visual, int floorY, HomeBiome matchedBiome, out int surfaceRelief)
		{
			surfaceRelief = -1;
			if (contract.BroadLayer == BroadLayoutLayer.Surface &&
				(AuthoredResidenceLayoutPolicy.SurfaceContactOutOfRange(contract, floorY - (int)Main.worldSurface) ||
					area.Y < 1 || mutationMask.ProtectedFootprint(area.Location, profile.ProtectedPadding).Top < AuthoredResidenceLayoutPolicy.WorldMargin))
			{
				return RejectTerrain("surface-altitude", $"x={area.X},floor={floorY},worldSurface={Main.worldSurface},assetY={area.Y}");
			}

			if (profile.TargetBiome == HomeBiome.Ocean && profile.TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope)
			{
				bool openSkyCoastalGrade = HasOpenSkyCoastalTerrain(visual);
				bool nearbyOpenWater = HasNearbyOpenCoastalWater(visual, surfaceIndex);
				// A terrain-envelope home can sit on an exposed coastal grade or beside
				// open water. Requiring both made legitimate modded shores domain-empty;
				// the exact Ocean biome, world bounds, critical anchors, protected masks,
				// relief, and global layout safety gates still apply below.
				if (!openSkyCoastalGrade && !nearbyOpenWater)
				{
					return RejectTerrain("coastal-exposure", $"x={area.X},y={area.Y}");
				}
			}

			if (contract.BroadLayer == BroadLayoutLayer.Surface && profile.TargetAnchor != DeploymentTargetAnchor.CoastalWaterlineEnvelope && surfaceIndex != null)
			{
				int maximumRelief = MaximumAllowedRelief();
				if (!surfaceIndex.TryMeasureRelief(visual.Left, visual.Right, out surfaceRelief) || surfaceRelief > maximumRelief)
				{
					return RejectTerrain("surface-visual-relief", $"left={visual.Left},right={visual.Right},relief={surfaceRelief},limit={maximumRelief}");
				}
			}

			if (surfaceIndex != null && !HasSuitableBiomeAlongExteriorContour(area, matchedBiome))
				return RejectTerrain("surface-contour-biome", $"x={area.X},y={area.Y},biome={matchedBiome}");

			return true;
		}

	}
}
