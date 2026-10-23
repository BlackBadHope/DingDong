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
	public static bool TryScore(AuthoredResidencePlacementContract contract, AuthoredPreset preset, AuthoredSceneMutationMask mask, CandidateDeploymentSite candidate, IReadOnlyList<CandidateDeploymentSite> planned, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, out int score, out string rejectReason, bool validateHardPairwise = true)
	{
		score = 0;
		rejectReason = string.Empty;
		if (contract.BroadLayer == BroadLayoutLayer.Surface)
		{
			int depth = candidate.TargetReference.Y - (int)Main.worldSurface;
			if (SurfaceContactOutOfRange(contract, depth) || SurfaceAssetTooHigh(candidate) || candidate.AltitudeBand != ResidenceAltitudeBand.Surface)
			{
				rejectReason = $"surface-contact:{depth}";
				return false;
			}
		}

		int sector = SectorOf(candidate.AssetArea.Center.X);
		BroadLayoutLayer layer = contract.BroadLayer;
		int count = 0;
		int mass = contract.Mass;
		int independent = contract.RequiresIsolation ? 1 : 0;
		int mega = contract.ScaleClass == ResidenceScaleClass.Mega ? 1 : 0;
		int landmarksInCell = contract.ScaleClass == ResidenceScaleClass.Landmark ? 1 : 0;
		int landmarkMega = IsLandmarkOrMega(contract) ? 1 : 0;
		int nearest = int.MaxValue;
		int nearby = 0;
		int skySameBand = 0;
		int altitudeSameBand = 0;
		int highSkyCeiling = IsHighSkyCeiling(candidate) ? 1 : 0;
		int leftLayer = 0;
		int rightLayer = 0;
		int leftOcean = 0;
		int rightOcean = 0;
		int preferredHits = candidate.Biome == contract.PreferredBiome ? 1 : 0;
		List<int> layerXs = new();
		Rectangle focus = mask.VisualFootprint(candidate.AssetArea.Location, 0);
		if (!TryEvaluatePlannedSiteInteractions(contract, candidate, planned, allMasks, ref score, ref rejectReason, validateHardPairwise, sector, layer, ref count, ref mass, ref independent, ref mega, ref landmarksInCell, ref landmarkMega, ref nearest, ref nearby, ref skySameBand, ref altitudeSameBand, ref highSkyCeiling, ref leftLayer, ref rightLayer, ref leftOcean, ref rightOcean, layerXs, focus))
			return false;
		if (!TryValidateCandidateCapacity(contract, candidate, ref score, ref rejectReason, sector, layer, count, mass, independent, mega, landmarksInCell, landmarkMega, nearby, altitudeSameBand, highSkyCeiling))
			return false;
		score = ApplyCandidateDistributionScore(contract, candidate, score, layer, count, mass, nearest, nearby, skySameBand, altitudeSameBand, leftLayer, rightLayer, leftOcean, rightOcean, preferredHits, layerXs);
		int emptySectorBonus = 0;
		bool sectorUsed = false;
		foreach (CandidateDeploymentSite existing in planned)
		{
			if (BroadLayerOf(AuthoredResidencePlacementManifest.Required(existing.NpcKey)) != layer)
				continue;
			if (SectorOf(existing.AssetArea.Center.X) == sector)
			{
				sectorUsed = true;
				break;
			}
		}

		if (!sectorUsed)
			emptySectorBonus = IsLandmarkOrMega(contract) ? 900 : 360;
		score += emptySectorBonus;
		score += Math.Abs(SectorOf(candidate.AssetArea.Center.X) - (MacroSectorCount - 1) / 2) * 4;
		score += BiomeBandBonus(contract.PreferredBiome, candidate.AssetArea.Center.X);
		return true;
	}

	public static int BiomeBandBonus(HomeBiome biome, int x)
	{
		int width = Math.Max(1, Main.maxTilesX);
		int leftBias = (width - x) * 180 / width;
		int rightBias = x * 180 / width;
		int mid = width / 2;
		int centerBias = (width / 2 - Math.Abs(x - mid)) * 160 / width;
		int forestCenter = width * 2 / 5;
		int forestBias = (width / 2 - Math.Abs(x - forestCenter)) * 200 / width;
		return biome switch
		{
			HomeBiome.Jungle => leftBias,
			HomeBiome.Ocean => x < mid ? leftBias : rightBias,
			HomeBiome.Forest => forestBias,
			HomeBiome.Desert => centerBias,
			HomeBiome.Snow => rightBias,
			HomeBiome.Hallow => rightBias,
			_ => 0
		};
	}
}
