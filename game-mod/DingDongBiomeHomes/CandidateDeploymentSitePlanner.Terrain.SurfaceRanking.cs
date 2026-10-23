using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		private bool TryRankCandidate(CandidateDeploymentSite candidate, int persistentOverlaps, int left, int rangeMin, int rangeMax, out CandidateDeploymentSite site)
		{
			if (persistentOverlaps == 0)
			{
				if (!spreadLayout)
				{
					site = candidate;
					return true;
				}
				if (!TryScore(candidate, out int score))
				{
					site = null;
					return false;
				}

				score += BiomeEdgeBonus(contract, left, rangeMin, rangeMax);
				ConsiderRanked(candidate, score);
				if (score > bestCleanScore)
				{
					bestCleanScore = score;
					bestCleanSite = candidate;
				}
				site = null;
				return false;
			}

			persistentCandidates++;
			if (!ContainsOnlyRelocatableChestCollisions(candidate, out string reason))
			{
				unsupportedPersistentCandidates++;
				if (!unsupportedPersistentReasons.TryAdd(reason, 1))
					unsupportedPersistentReasons[reason]++;
				site = null;
				return false;
			}

			int advisoryScore = int.MinValue;
			if (spreadLayout)
			{
				if (!TryScore(candidate, out advisoryScore))
				{
					site = null;
					return false;
				}
				advisoryScore += BiomeEdgeBonus(contract, left, rangeMin, rangeMax);
				ConsiderRanked(candidate, advisoryScore - 400);
			}

			if (persistentOverlaps < bestPersistentOverlaps || persistentOverlaps == bestPersistentOverlaps && advisoryScore > bestAdvisoryScore)
			{
				bestPersistentOverlaps = persistentOverlaps;
				bestAdvisoryScore = advisoryScore;
				bestAdvisorySite = candidate;
			}
			site = null;
			return false;
		}

		private bool TryScore(CandidateDeploymentSite candidate, out int score)
		{
			if (AuthoredResidenceLayoutPolicy.TryScore(contract, preset, mutationMask, candidate, plannedSites, allMasks, out score, out string rejectReason))
				return true;
			CountScoreReject(rejectReason, ref spacingRejected, ref independentRejected, ref megaRejected, ref densityRejected, ref massRejected, ref landmarkRejected);
			return false;
		}
	}
}
