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
	/// <summary>
	/// Fast monotone capacity gate for forward propagation. Pairwise geometry is
	/// validated separately; scoring-only distance and cadence work deliberately
	/// stays out of this hot path.
	/// </summary>
	public static bool TryValidateAggregateCapacity(AuthoredResidencePlacementContract contract, CandidateDeploymentSite candidate, IReadOnlyList<CandidateDeploymentSite> planned, out string rejectReason)
	{
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
		int mass = contract.Mass;
		int sitesInCell = 1;
		int independent = contract.RequiresIsolation ? 1 : 0;
		int mega = contract.ScaleClass == ResidenceScaleClass.Mega ? 1 : 0;
		int landmarksInCell = contract.ScaleClass == ResidenceScaleClass.Landmark ? 1 : 0;
		int landmarkMega = IsLandmarkOrMega(contract) ? 1 : 0;
		int altitudeBandSites = 1;
		int highSkyCeiling = IsHighSkyCeiling(candidate) ? 1 : 0;
		foreach (CandidateDeploymentSite existing in planned)
		{
			AuthoredResidencePlacementContract other = AuthoredResidencePlacementManifest.Required(existing.NpcKey);
			if (other.BroadLayer == layer && SectorOf(existing.AssetArea.Center.X) == sector)
			{
				sitesInCell++;
				mass += other.Mass;
				if (other.RequiresIsolation)
					independent++;
				if (existing.Biome == candidate.Biome && other.ScaleClass == ResidenceScaleClass.Mega)
					mega++;
				if (existing.Biome == candidate.Biome && other.ScaleClass == ResidenceScaleClass.Landmark)
					landmarksInCell++;
			}

			if (other.BroadLayer == layer && existing.AltitudeBand == candidate.AltitudeBand)
				altitudeBandSites++;
			if (IsHighSkyCeiling(existing))
				highSkyCeiling++;
			if (other.BroadLayer == layer && IsLandmarkOrMega(other) && IsLandmarkOrMega(contract) && other.PreferredBiome == contract.PreferredBiome && Math.Abs(SectorOf(existing.AssetArea.Center.X) - sector) <= 1)
			{
				landmarkMega++;
			}
		}

		if (sitesInCell > MaxSitesPerSector(layer))
		{
			rejectReason = $"site-density:{sector}/{layer}:{sitesInCell}";
			return false;
		}

		if (mega > MaxMegaPerSectorLayer)
		{
			rejectReason = $"mega-sector:{sector}/{layer}";
			return false;
		}

		if (mega > 0 && landmarksInCell > 0)
		{
			rejectReason = $"landmark-mega-sector:{sector}/{layer}";
			return false;
		}

		if (highSkyCeiling > MaxHighSkySitesAtCeiling)
		{
			rejectReason = $"high-sky-ceiling:{highSkyCeiling}";
			return false;
		}

		if (landmarkMega > MaxAdjacentLandmarkMega && IsLandmarkOrMega(contract))
		{
			rejectReason = $"landmark-adjacent:{sector}/{layer}";
			return false;
		}

		if (independent > MaxIndependentPerSector(layer))
		{
			rejectReason = $"independent-density:{sector}/{layer}:{independent}";
			return false;
		}

		if (mass > MaxSectorMass)
		{
			rejectReason = $"sector-mass:{sector}/{layer}:{mass}";
			return false;
		}

		if (layer == BroadLayoutLayer.Sky && altitudeBandSites > MaxSkySitesPerAltitudeBand)
		{
			rejectReason = $"sky-band-density:{candidate.AltitudeBand}:{altitudeBandSites}";
			return false;
		}

		return true;
	}
}
