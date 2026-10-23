using System;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class RankedCandidateCollector
	{
		public void Consider(CandidateDeploymentSite site, int score)
		{
			// Admission precedes score and bucket limits. Otherwise blocked high-score
			// sites can evict every lower-score site that StructureMap can reserve.
			if (admission != null && !admission(site))
				return;
			AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(site.NpcKey);
			int sector = AuthoredResidenceLayoutPolicy.SectorOf(site.AssetArea.Center.X);
			int altitudeBucket = contract.BroadLayer == BroadLayoutLayer.Sky ? site.AssetArea.Y / 64 : 0;
			bool tightPacking = contract.MacroWindow == ResidenceMacroWindow.HallowEitherShelf || contract.PreferredBiome is HomeBiome.Ocean or HomeBiome.Snow or HomeBiome.Desert or HomeBiome.Jungle;
			int neighborhood = tightPacking ? tightNeighborhood : Math.Max(normalNeighborhood, site.AssetArea.Width / (normalNeighborhood >= 96 ? 3 : 8));
			int candidatesPerBucket = tightPacking ? tightCandidatesPerBucket : normalCandidatesPerBucket;
			FindBucketPlacement(site, contract, sector, altitudeBucket, neighborhood, out int sameBucketCount, out int weakestIndex, out int weakestScore, out int nearbyIndex);
			if (nearbyIndex >= 0)
			{
				if (score > Items[nearbyIndex].Score)
				{
					Items[nearbyIndex] = (site, score);
					Sort();
				}
				return;
			}
			if (sameBucketCount >= candidatesPerBucket)
			{
				if (weakestIndex >= 0 && score > weakestScore)
				{
					Items[weakestIndex] = (site, score);
					Sort();
				}
				return;
			}
			if (Items.Count < limit)
			{
				Items.Add((site, score));
				Sort();
				return;
			}
			if (score > Items[^1].Score)
			{
				Items[^1] = (site, score);
				Sort();
			}
		}

		private void FindBucketPlacement(CandidateDeploymentSite site, AuthoredResidencePlacementContract contract, int sector, int altitudeBucket, int neighborhood, out int sameBucketCount, out int weakestIndex, out int weakestScore, out int nearbyIndex)
		{
			sameBucketCount = 0;
			weakestIndex = -1;
			weakestScore = int.MaxValue;
			nearbyIndex = -1;
			for (int i = 0; i < Items.Count; i++)
			{
				CandidateDeploymentSite existing = Items[i].Site;
				AuthoredResidencePlacementContract existingContract = AuthoredResidencePlacementManifest.Required(existing.NpcKey);
				if (existingContract.BroadLayer != contract.BroadLayer || AuthoredResidenceLayoutPolicy.SectorOf(existing.AssetArea.Center.X) != sector || contract.BroadLayer == BroadLayoutLayer.Sky && existing.AssetArea.Y / 64 != altitudeBucket)
					continue;
				sameBucketCount++;
				if (Items[i].Score < weakestScore)
				{
					weakestScore = Items[i].Score;
					weakestIndex = i;
				}
				if (Math.Abs(existing.AssetArea.Center.X - site.AssetArea.Center.X) < neighborhood && Math.Abs(existing.AssetArea.Y - site.AssetArea.Y) < 64)
				{
					nearbyIndex = i;
					return;
				}
			}
		}
	}
}
