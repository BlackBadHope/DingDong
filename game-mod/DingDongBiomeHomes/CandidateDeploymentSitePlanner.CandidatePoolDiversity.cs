using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConstraintCandidatePoolBuilder
	{
		private static List<RankedLayoutCandidate> PruneSpatialBuckets(
			AuthoredDeploymentProfile profile, List<RankedLayoutCandidate> ordered)
		{
			AuthoredResidencePlacementContract contract =
				AuthoredResidencePlacementManifest.Required(profile.NpcKey);
			Dictionary<(int Sector, int XBucket, int YBucket), int> perBucket = new();
			Dictionary<int, List<RankedLayoutCandidate>> bySector = new();
			foreach (RankedLayoutCandidate candidate in ordered)
			{
				int sector = AuthoredResidenceLayoutPolicy.SectorOf(
					candidate.Site.AssetArea.Center.X);
				var key = (sector, candidate.Site.AssetArea.Center.X / 96,
					contract.BroadLayer == BroadLayoutLayer.Surface
						? 0 : candidate.Site.AssetArea.Center.Y / 64);
				perBucket.TryGetValue(key, out int count);
				if (count >= ConstraintCandidatesPerSpatialBucket)
					continue;
				perBucket[key] = count + 1;
				if (!bySector.TryGetValue(sector, out List<RankedLayoutCandidate> sectorPool))
				{
					sectorPool = new List<RankedLayoutCandidate>();
					bySector.Add(sector, sectorPool);
				}
				sectorPool.Add(candidate);
			}

			List<int> sectors = new(bySector.Keys);
			sectors.Sort();
			List<RankedLayoutCandidate> pruned = new(ConstraintCandidatePoolLimit);
			for (int depth = 0; pruned.Count < ConstraintCandidatePoolLimit; depth++)
			{
				bool added = false;
				foreach (int sector in sectors)
				{
					List<RankedLayoutCandidate> sectorPool = bySector[sector];
					if (depth >= sectorPool.Count)
						continue;
					pruned.Add(sectorPool[depth]);
					added = true;
					if (pruned.Count >= ConstraintCandidatePoolLimit)
						break;
				}
				if (!added)
					break;
			}
			return pruned;
		}
	}
}
