using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		private bool TryScanRange(int min, int max, out CandidateDeploymentSite site, bool refine = false, int? edgeAnchorOffset = null, Worldgen.BoundedRefinementBudget edgeBudget = null)
		{
			foreach (int left in Worldgen.SurfaceCandidateSampling.Columns(min, max,
				seed, HorizontalStep(), spreadLayout, refine))
			{
				if (edgeBudget != null && !edgeBudget.TryConsume()) break;
				if (TryScanHorizontalSample(left, min, max, out site, edgeAnchorOffset))
					return true;
			}

			site = null;
			return false;
		}

		private int HorizontalStep()
		{
			if (!spreadLayout)
				return 1;
			bool constrained = profile.TargetBiome is HomeBiome.Ocean or HomeBiome.Jungle or HomeBiome.Snow or HomeBiome.Desert or HomeBiome.Mushroom
				|| profile.TargetAnchor == DeploymentTargetAnchor.CoastalWaterlineEnvelope
				|| contract.ScaleClass is ResidenceScaleClass.Mega or ResidenceScaleClass.Landmark;
			if (contract.ScaleClass is ResidenceScaleClass.Mega or ResidenceScaleClass.Landmark)
				return 4;
			return constrained ? 5 : Math.Max(4, Math.Min(8, Math.Max(1, preset.Width / 12)));
		}

		private bool TryScanHorizontalSample(int left, int rangeMin, int rangeMax, out CandidateDeploymentSite site, int? edgeAnchorOffset = null)
		{
			if (!ReplayColumn(profile.NpcKey, left)) { site = null; return false; }
			activeCandidateDomainAudit?.Column(profile.NpcKey, left);
			if (edgeAnchorOffset.HasValue)
			{
				// Fit a broad authored terrain envelope to real supported contacts
				// across its footprint, not just the possibly low centre of a valley.
				// No random Y offsets and no relaxation of relief/biome/write gates.
				// Equal-height contacts can have different biomes. Do not suppress
				// an admitted edge contact merely because the rejected centre has
				// the same floor Y; the pool collector deduplicates final geometry.
				int anchorX = left + edgeAnchorOffset.Value;
				if (surfaceIndex.TryFloor(anchorX, out int anchorY) &&
					TryMatchAllowedBiome(anchorX, anchorY, profile, contract, plannedSites, out HomeBiome biome, out string match) &&
					ReplayArea(profile.NpcKey, AreaAt(profile, preset, mutationMask, left, anchorY, layoutSalt, contract.PreferredAltitudeBand)) &&
					TryEvaluateBand(left, rangeMin, rangeMax, anchorX, anchorY, biome, match, contract.PreferredAltitudeBand, out site))
					return true;
				site = null;
				return false;
			}
			int centerX = left + AuthoredSourceContext.ReferenceCenterOffsetX(preset);
			bool oceanSky = profile.TargetBiome == HomeBiome.Ocean &&
				profile.TargetAnchor == DeploymentTargetAnchor.SkyClearanceEnvelope;
			int oceanSurfaceY = 0;
			bool foundOceanSurface = false;
			if (oceanSky)
			{
				// Classify an ocean island against the exposed water, not a seabed
				// which may be far below it and contain no nearby water samples.
				// Keep the terrestrial reference below as another admissible option;
				// both references use the same biome, sky and write-safety gates.
				foundOceanSurface = surfaceIndex != null && surfaceIndex.TryWaterline(centerX, out oceanSurfaceY);
				if (!foundOceanSurface)
					foundOceanSurface = TryFindCoastalWaterline(centerX, out oceanSurfaceY, allowCoastalSkyDomain: true);
				if (foundOceanSurface && TryScanSurfaceReference(left, rangeMin, rangeMax, centerX, oceanSurfaceY, out site))
					return true;
			}

			bool waterline = profile.TargetAnchor == DeploymentTargetAnchor.CoastalWaterlineEnvelope;
			int floorY = 0;
			bool foundReference = surfaceIndex != null && (waterline ? surfaceIndex.TryWaterline(centerX, out floorY) : surfaceIndex.TryFloor(centerX, out floorY));
			if (!foundReference)
				foundReference = waterline ? TryFindCoastalWaterline(centerX, out floorY) : TryFindSurfaceFloor(centerX, out floorY);
			if (!foundReference)
			{
				if (!foundOceanSurface)
				{
					activeCandidateDomainAudit?.Rejection(profile.NpcKey, "reference-not-found", $"centerX={centerX}");
					biomeRejected++;
				}
				site = null;
				return false;
			}
			if (foundOceanSurface && floorY == oceanSurfaceY)
			{
				site = null;
				return false;
			}
			return TryScanSurfaceReference(left, rangeMin, rangeMax, centerX, floorY, out site);
		}

		private bool TryScanSurfaceReference(int left, int rangeMin, int rangeMax, int centerX, int floorY, out CandidateDeploymentSite site)
		{
			if (!TryMatchAllowedBiome(centerX, floorY, profile, contract, plannedSites, out HomeBiome matchedBiome, out string fallback))
			{
				activeCandidateDomainAudit?.Rejection(profile.NpcKey, "biome-mismatch", $"centerX={centerX},referenceY={floorY}");
				biomeRejected++;
				site = null;
				return false;
			}

			foreach (ResidenceAltitudeBand band in AllowedAltitudeBands())
			{
				if (!ReplayArea(profile.NpcKey, AreaAt(profile, preset, mutationMask, left, floorY, layoutSalt, band)))
					continue;
				if (TryEvaluateBand(left, rangeMin, rangeMax, centerX, floorY, matchedBiome, fallback, band, out site))
					return true;
			}

			site = null;
			return false;
		}

		private IReadOnlyList<ResidenceAltitudeBand> AllowedAltitudeBands() =>
			profile.TargetAnchor == DeploymentTargetAnchor.SkyClearanceEnvelope
				? contract.AllowedLayers
				: new[] { contract.PreferredAltitudeBand };
	}
}
