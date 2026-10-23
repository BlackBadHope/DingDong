using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;
/// <summary>
/// Deterministic spatial scoring and fail-closed distribution acceptance.
/// Collision clearance stays at 12 tiles; visual spacing is a separate, larger
/// constraint derived from authored focus size and scale class.
/// </summary>
public static partial class AuthoredResidenceLayoutPolicy
{
	private static int ApplyCandidateDistributionScore(AuthoredResidencePlacementContract contract, CandidateDeploymentSite candidate, int score, BroadLayoutLayer layer, int count, int mass, int nearest, int nearby, int skySameBand, int altitudeSameBand, int leftLayer, int rightLayer, int leftOcean, int rightOcean, int preferredHits, List<int> layerXs)
	{
		score += preferredHits * 800;
		score += Math.Max(0, contract.MaximumLocalDensity - nearby) * 140;
		score += Math.Max(0, MaxSectorMass - mass) * 25;
		score -= count * 90;
		if (nearest != int.MaxValue)
			score += Math.Min(nearest, 900) / 4;
		if (contract.RequiresIsolation && nearest != int.MaxValue)
			score += Math.Min(nearest, 1400) / 3;
		if (candidate.AltitudeBand == contract.PreferredAltitudeBand)
			score += 240;
		else
			score -= 160;
		if (IsHighSkyCeiling(candidate))
			score -= 420;
		if (layer == BroadLayoutLayer.Surface)
		{
			int depth = candidate.TargetReference.Y - (int)Main.worldSurface;
			score += Math.Max(0, MaximumSurfaceElevationAboveWorldSurface - Math.Max(0, -depth)) * 3;
		}

		score -= skySameBand * 220;
		score -= altitudeSameBand * (layer is BroadLayoutLayer.Sky or BroadLayoutLayer.Cavern ? 180 : 0);
		bool candidateLeft = candidate.AssetArea.Center.X < Main.maxTilesX / 2;
		int candidateSide = candidateLeft ? leftLayer : rightLayer;
		int oppositeSide = candidateLeft ? rightLayer : leftLayer;
		if (candidateSide < oppositeSide)
			score += 260;
		else if (candidateSide > oppositeSide && layer is BroadLayoutLayer.Sky or BroadLayoutLayer.Cavern or BroadLayoutLayer.Underworld)
			score -= 260;
		score += contract.CadenceWeight * 10;
		if (contract.PreferredBiome == HomeBiome.Ocean || candidate.Biome == HomeBiome.Ocean)
		{
			bool left = candidate.AssetArea.Center.X < Main.maxTilesX / 2;
			int ours = left ? leftOcean : rightOcean;
			int otherSide = left ? rightOcean : leftOcean;
			if (ours > otherSide)
				score -= 420;
			else if (ours < otherSide)
				score += 480;
			if (contract.RequiresIsolation && ours > 0)
				score -= 360;
			if (contract.ClusterPolicy == ResidenceClusterPolicy.Port && otherSide == 0 && ours > 1)
				score -= 200;
		}

		if (layerXs.Count > 0)
		{
			layerXs.Sort();
			int leftEdge = WorldMargin + 80;
			int rightEdge = Main.maxTilesX - WorldMargin - 80;
			int insert = candidate.AssetArea.Center.X;
			int prev = leftEdge;
			int maxGap = 0;
			int gapAtInsert = 0;
			foreach (int x in layerXs)
			{
				int gap = x - prev;
				if (gap > maxGap)
					maxGap = gap;
				if (insert >= prev && insert <= x)
					gapAtInsert = gap;
				prev = x;
			}

			int tail = rightEdge - prev;
			if (tail > maxGap)
				maxGap = tail;
			if (insert >= prev)
				gapAtInsert = tail;
			if (maxGap > 0)
				score += (int)(320.0 * gapAtInsert / maxGap);
		}

		return score;
	}
}
