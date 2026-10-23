using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CavernPlanningSearch
	{
		private bool TrySample(int attempt, out CandidateDeploymentSite site)
		{
			(int minX, int maxX) = horizontalRanges[(seed + attempt) % horizontalRanges.Count];
			int xCount = Math.Max(1, maxX - minX + 1);
			int left = minX + (seed + attempt / horizontalRanges.Count * 53) % xCount;
			if (!ReplayColumn(profile.NpcKey, left)) return Reject(out site);
			(ResidenceAltitudeBand band, int minY, int maxY) = altitudeRanges[(seed + attempt) % altitudeRanges.Count];
			int yCount = Math.Max(1, maxY - minY + 1);
			int top = minY + (seed / 17 + attempt / altitudeRanges.Count * 29) % yCount;
			if (requireReviewedGroundSupport && !TryAlignReviewedCavernGroundTop(left, top, preset, profile.SourceGroundOffset.Value, minY, maxY, out top))
				return Reject(out site);

			Rectangle area = new(left, top, preset.Width, preset.Height);
			if (!ReplayArea(profile.NpcKey, area)) return Reject(out site);
			Rectangle protectedArea = mutationMask.ProtectedFootprint(area.Location, profile.ProtectedPadding);
			if (!InWorld(protectedArea) || IntersectsPlannedVisualFocus(area, mutationMask, plannedSites, allMasks, contract.BroadLayer) ||
				IntersectsReserved(protectedArea, reserved) && IntersectsPlannedMutations(area, mutationMask, plannedSites, allMasks))
				return Reject(out site);

			if (!LooksLikeOrganicCavern(protectedArea, profile.TargetAnchor == DeploymentTargetAnchor.OrganicMushroomCavernEnvelope) ||
				!HasCompatibleTerrainAlongExteriorContour(area, mutationMask, profile.TargetBiome) ||
				requireReviewedGroundSupport && !HasReviewedCavernGroundSupport(area, profile.SourceGroundOffset.Value) || ContainsWorldCriticalAnchor(protectedArea))
				return Reject(out site);

			int overlaps = CountPersistentObjects(protectedArea, area, profile.BlendMargin, mutationMask, out IReadOnlyList<PersistentObjectCollision> collisions);
			CandidateDeploymentSite candidate = new(profile.NpcKey, profile.PresetId, profile.TargetBiome, area, protectedArea, new Point(area.Center.X, area.Center.Y), "ExactBiomeOnly", collisions, band, -1);
			return TryRank(candidate, overlaps, out site);
		}

		private bool TryRank(CandidateDeploymentSite candidate, int persistentOverlaps, out CandidateDeploymentSite site)
		{
			if (persistentOverlaps == 0)
			{
				if (!spreadLayout)
				{
					site = candidate;
					return true;
				}
				if (!AuthoredResidenceLayoutPolicy.TryScore(contract, preset, mutationMask, candidate, plannedSites, allMasks, out int score, out _))
					return Reject(out site);
				ConsiderRanked(candidate, score);
				if (score > bestCleanScore)
				{
					bestCleanScore = score;
					bestCleanSite = candidate;
				}
				site = null;
				return false;
			}

			if (!ContainsOnlyRelocatableChestCollisions(candidate, out _))
			{
				site = null;
				return false;
			}
			int advisoryScore = int.MinValue;
			if (spreadLayout && !AuthoredResidenceLayoutPolicy.TryScore(contract, preset, mutationMask, candidate, plannedSites, allMasks, out advisoryScore, out _))
			{
				site = null;
				return false;
			}
			if (spreadLayout)
				ConsiderRanked(candidate, advisoryScore - 400);
			if (persistentOverlaps < bestPersistentOverlaps || persistentOverlaps == bestPersistentOverlaps && advisoryScore > bestAdvisoryScore)
			{
				bestPersistentOverlaps = persistentOverlaps;
				bestAdvisoryScore = advisoryScore;
				bestAdvisorySite = candidate;
			}
			site = null;
			return false;
		}

		private bool Reject(out CandidateDeploymentSite site)
		{
			rejected++;
			site = null;
			return false;
		}
	}
}
