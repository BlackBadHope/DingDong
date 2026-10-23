using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConstraintCandidatePoolBuilder
	{
		private void TrackProfilePool(AuthoredDeploymentProfile profile, List<RankedLayoutCandidate> pool)
		{
			Dictionary<int, int> sectorCounts = CountSectors(pool);
			foreach (int sectorCount in sectorCounts.Values)
			{
				sectorDomainTotal += sectorCount;
				sectorDomainCount++;
				sectorDomainMinimum = Math.Min(sectorDomainMinimum, sectorCount);
				sectorDomainMaximum = Math.Max(sectorDomainMaximum, sectorCount);
			}

			List<int> sectors = new(sectorCounts.Keys);
			sectors.Sort();
			List<string> loggedCounts = new(sectors.Count);
			foreach (int sector in sectors)
				loggedCounts.Add($"{sector}:{sectorCounts[sector]}");

			FindBounds(pool, out int minX, out int maxX, out int minY, out int maxY);
			AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(profile.NpcKey);
			mod.Logger.Info($"DINGDONG_PLANNER_DOMAIN|npc={profile.NpcKey}|layer={contract.BroadLayer}|count={pool.Count}|sectors={string.Join(',', loggedCounts)}|x={minX}..{maxX}|y={minY}..{maxY}");

			poolDigest = MixPoolDigest(poolDigest, profile.NpcKey);
			foreach (RankedLayoutCandidate candidate in pool)
			{
				poolDigest = MixPoolDigest(poolDigest, candidate.Site.AssetArea.X);
				poolDigest = MixPoolDigest(poolDigest, candidate.Site.AssetArea.Y);
				poolDigest = MixPoolDigest(poolDigest, candidate.Site.AssetArea.Width);
				poolDigest = MixPoolDigest(poolDigest, candidate.Site.AssetArea.Height);
				poolDigest = MixPoolDigest(poolDigest, (int)candidate.Site.Biome);
				poolDigest = MixPoolDigest(poolDigest, candidate.BaseScore);
			}
			total += pool.Count;
			minimum = Math.Min(minimum, pool.Count);
			maximum = Math.Max(maximum, pool.Count);
		}

		private static Dictionary<int, int> CountSectors(List<RankedLayoutCandidate> pool)
		{
			Dictionary<int, int> counts = new();
			foreach (RankedLayoutCandidate candidate in pool)
			{
				int sector = AuthoredResidenceLayoutPolicy.SectorOf(candidate.Site.AssetArea.Center.X);
				counts.TryGetValue(sector, out int count);
				counts[sector] = count + 1;
			}
			return counts;
		}

		private static void FindBounds(List<RankedLayoutCandidate> pool, out int minX, out int maxX, out int minY, out int maxY)
		{
			minX = int.MaxValue;
			maxX = int.MinValue;
			minY = int.MaxValue;
			maxY = int.MinValue;
			foreach (RankedLayoutCandidate candidate in pool)
			{
				minX = Math.Min(minX, candidate.Site.AssetArea.X);
				maxX = Math.Max(maxX, candidate.Site.AssetArea.X);
				minY = Math.Min(minY, candidate.Site.AssetArea.Y);
				maxY = Math.Max(maxY, candidate.Site.AssetArea.Y);
			}
		}
	}
}
