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
	private static bool TryPlanUnderworld(AuthoredDeploymentProfile profile, AuthoredPreset preset, AuthoredSceneMutationMask mutationMask, List<Rectangle> reserved, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, int layoutSalt, bool allowPersistentAdvisory, out CandidateDeploymentSite site, out string failure)
	{
		AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(profile.NpcKey);
		bool spreadLayout = layoutSalt > 0;
		int minX = 180;
		int maxX = Main.maxTilesX - 180 - preset.Width;
		// Follow the generated biome ceiling, while retaining the original
		// 230-cell allowance for a residence's roof to extend into the ash shelf.
		// Fixed bottom offsets otherwise strand all homes in the old thin layer.
		// Use the generating depth policy directly. Main.UnderworldLayer is a
		// hookable trivial getter: an already-inlined caller can retain vanilla
		// height-200 while the bottom clearance below is correctly scaled.
		// On 7200-high worlds that inverted the castle range (6770..6710),
		// and Math.Max(1, count) silently searched one invalid-height row.
		if (maxX < minX || !Worldgen.UnderworldCandidateDomain.TryVerticalRange(
			Main.maxTilesY, Worldgen.NativeUnderworldSystem.ScaleOffset(200),
			Worldgen.NativeUnderworldSystem.ScaleOffset(100), preset.Height,
			out int minY, out int maxY))
			return Fail("underworld-domain-empty:scene-or-clearance-does-not-fit", out site, out failure);
		int seed = PositiveHash($"underworld:{profile.NpcKey}:{PlanningSeedKey}:layout={layoutSalt}");
		int xCount = maxX - minX + 1;
		int yCount = maxY - minY + 1;
		int rejected = 0;
		CandidateDeploymentSite bestCleanSite = null;
		int bestCleanScore = int.MinValue;
		CandidateDeploymentSite bestAdvisorySite = null;
		int bestPersistentOverlaps = int.MaxValue;
		int bestAdvisoryScore = int.MinValue;
		int underworldAttempts = spreadLayout ? 3600 : 3600;
		for (int attempt = 0; attempt < underworldAttempts; attempt++)
		{
			int left = minX + ((seed + attempt * 43) % xCount);
			int top = minY + ((seed / 13 + attempt * 17) % yCount);
			Rectangle area = new(left, top, preset.Width, preset.Height);
			if (!ReplayArea(profile.NpcKey, area)) continue;
			Rectangle protectedArea = mutationMask.ProtectedFootprint(area.Location, profile.ProtectedPadding);
			if (!InWorld(protectedArea) || IntersectsPlannedVisualFocus(area, mutationMask, plannedSites, allMasks, contract.BroadLayer) || (IntersectsReserved(protectedArea, reserved) && IntersectsPlannedMutations(area, mutationMask, plannedSites, allMasks)) || !LooksLikeUnderworldShelf(protectedArea) || !HasCompatibleTerrainAlongExteriorContour(area, mutationMask, HomeBiome.Underworld) || ContainsWorldCriticalAnchor(protectedArea))
			{
				rejected++;
				continue;
			}

			int persistentOverlaps = CountPersistentObjects(protectedArea, area, profile.BlendMargin, mutationMask, out IReadOnlyList<PersistentObjectCollision> persistentCollisions);
			CandidateDeploymentSite candidate = new(profile.NpcKey, profile.PresetId, profile.TargetBiome, area, protectedArea, area.Center, "ExactBiomeOnly", persistentCollisions, ResidenceAltitudeBand.Underworld, -1);
			if (persistentOverlaps == 0)
			{
				if (!spreadLayout)
				{
					site = candidate;
					failure = string.Empty;
					return true;
				}

				if (!AuthoredResidenceLayoutPolicy.TryScore(contract, preset, mutationMask, candidate, plannedSites, allMasks, out int score, out _))
				{
					rejected++;
					continue;
				}

				ConsiderRanked(candidate, score);
				if (score > bestCleanScore)
				{
					bestCleanScore = score;
					bestCleanSite = candidate;
				}

				continue;
			}

			if (!ContainsOnlyRelocatableChestCollisions(candidate, out _))
				continue;
			int advisoryScore = int.MinValue;
			if (spreadLayout && !AuthoredResidenceLayoutPolicy.TryScore(contract, preset, mutationMask, candidate, plannedSites, allMasks, out advisoryScore, out _))
				continue;
			if (spreadLayout)
				ConsiderRanked(candidate, advisoryScore - 400);
			if (persistentOverlaps < bestPersistentOverlaps || (persistentOverlaps == bestPersistentOverlaps && advisoryScore > bestAdvisoryScore))
			{
				bestPersistentOverlaps = persistentOverlaps;
				bestAdvisoryScore = advisoryScore;
				bestAdvisorySite = candidate;
			}
		}

		if (bestCleanSite != null)
		{
			site = bestCleanSite;
			failure = string.Empty;
			return true;
		}

		if (allowPersistentAdvisory && bestAdvisorySite != null)
		{
			site = bestAdvisorySite;
			failure = string.Empty;
			return true;
		}

		return Fail($"underworld-site-not-found:rejected={rejected}", out site, out failure);
	}
}
