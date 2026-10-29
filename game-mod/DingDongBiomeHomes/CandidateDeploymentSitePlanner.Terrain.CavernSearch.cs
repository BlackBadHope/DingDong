using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CavernPlanningSearch
	{
		private readonly AuthoredDeploymentProfile profile;
		private readonly AuthoredPreset preset;
		private readonly AuthoredSceneMutationMask mutationMask;
		private readonly List<Rectangle> reserved;
		private readonly IReadOnlyList<CandidateDeploymentSite> plannedSites;
		private readonly IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks;
		private readonly bool allowPersistentAdvisory;
		private readonly AuthoredResidencePlacementContract contract;
		private readonly bool spreadLayout;
		private readonly List<(int Min, int Max)> horizontalRanges;
		private readonly List<(ResidenceAltitudeBand Band, int Min, int Max)> altitudeRanges;
		private readonly int seed;
		private readonly bool requireReviewedGroundSupport;
		private int rejected;
		private CandidateDeploymentSite bestCleanSite;
		private int bestCleanScore = int.MinValue;
		private CandidateDeploymentSite bestAdvisorySite;
		private int bestPersistentOverlaps = int.MaxValue;
		private int bestAdvisoryScore = int.MinValue;

		public CavernPlanningSearch(AuthoredDeploymentProfile profile, AuthoredPreset preset, AuthoredSceneMutationMask mutationMask, List<Rectangle> reserved, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, SurfaceColumnIndex surfaceIndex, int layoutSalt, bool allowPersistentAdvisory)
		{
			this.profile = profile;
			this.preset = preset;
			this.mutationMask = mutationMask;
			this.reserved = reserved;
			this.plannedSites = plannedSites;
			this.allMasks = allMasks;
			this.allowPersistentAdvisory = allowPersistentAdvisory;
			contract = AuthoredResidencePlacementManifest.Required(profile.NpcKey);
			spreadLayout = layoutSalt > 0;
			horizontalRanges = CollectHorizontalRanges(profile, contract, preset.Width, surfaceIndex);
			ClipHorizontalRanges();
			altitudeRanges = CavernAltitudeRanges(contract, preset.Height);
			seed = PositiveHash($"cavern:{profile.NpcKey}:{PlanningSeedKey}:layout={layoutSalt}");
			requireReviewedGroundSupport = profile.SourceAnchor == DeploymentSourceAnchor.ReviewedGroundRow && profile.SourceGroundOffset.HasValue;
		}

		public bool TryFind(out CandidateDeploymentSite site, out string failure)
		{
			if (horizontalRanges.Count == 0)
				return Fail("cavern-horizontal-domain-empty", out site, out failure);
			if (altitudeRanges.Count == 0)
				return Fail("cavern-altitude-domain-empty", out site, out failure);
			if (TryFindCompilerCavern(out site, out bool compilerDomain, out failure))
				return true;
			if (compilerDomain)
				return false;

			int attempts = spreadLayout ? 1800 : 6000;
			for (int attempt = 0; attempt < attempts; attempt++)
			{
				if (TrySample(attempt, out site))
				{
					failure = string.Empty;
					return true;
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
			return Fail($"cavern-site-not-found:rejected={rejected}", out site, out failure);
		}

		private void ClipHorizontalRanges()
		{
			for (int i = horizontalRanges.Count - 1; i >= 0; i--)
			{
				(int min, int max) = horizontalRanges[i];
				min = Math.Max(180, min);
				max = Math.Min(Main.maxTilesX - 180 - preset.Width, max);
				if (min > max)
					horizontalRanges.RemoveAt(i);
				else
					horizontalRanges[i] = (min, max);
			}
		}
	}
}
