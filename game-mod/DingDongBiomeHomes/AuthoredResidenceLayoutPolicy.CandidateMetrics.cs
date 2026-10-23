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
	private static void AccumulatePlannedSiteMetrics(AuthoredResidencePlacementContract contract, CandidateDeploymentSite candidate, ref int score, int sector, BroadLayoutLayer layer, ref int count, ref int mass, ref int independent, ref int mega, ref int landmarksInCell, ref int landmarkMega, ref int altitudeSameBand, ref int highSkyCeiling, ref int leftLayer, ref int rightLayer, ref int leftOcean, ref int rightOcean, List<int> layerXs, CandidateDeploymentSite existing, AuthoredResidencePlacementContract other, int existingSector, BroadLayoutLayer existingLayer)
	{
		if (contract.ScaleClass == ResidenceScaleClass.Mega && other.ScaleClass == ResidenceScaleClass.Mega && contract.BroadLayer == BroadLayoutLayer.Surface && other.BroadLayer == BroadLayoutLayer.Surface)
		{
			int megaDx = Math.Abs(candidate.AssetArea.Center.X - existing.AssetArea.Center.X);
			if (megaDx < MinimumSurfaceMegaCenterSeparation)
				score -= (MinimumSurfaceMegaCenterSeparation - megaDx) * 12;
			else
				score += Math.Min(megaDx, 1200) / 4;
		}

		if (IsHighSkyCeiling(existing))
			highSkyCeiling++;
		if (existingLayer == layer)
		{
			if (existing.AssetArea.Center.X < Main.maxTilesX / 2)
				leftLayer++;
			else
				rightLayer++;
			if (existing.AltitudeBand == candidate.AltitudeBand)
				altitudeSameBand++;
			layerXs.Add(existing.AssetArea.Center.X);
			if (existingSector == sector)
			{
				count++;
				mass += other.Mass;
				if (other.RequiresIsolation)
					independent++;
				if (existing.Biome == candidate.Biome && other.ScaleClass == ResidenceScaleClass.Mega)
					mega++;
				if (existing.Biome == candidate.Biome && other.ScaleClass == ResidenceScaleClass.Landmark)
					landmarksInCell++;
			}

			if (IsLandmarkOrMega(other) && IsLandmarkOrMega(contract) && other.PreferredBiome == contract.PreferredBiome && Math.Abs(existingSector - sector) <= 1)
			{
				landmarkMega++;
			}
		}

		if (existing.Biome == HomeBiome.Ocean)
		{
			if (existing.AssetArea.Center.X < Main.maxTilesX / 2)
				leftOcean++;
			else
				rightOcean++;
		}
	}
}
