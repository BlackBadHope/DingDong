namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
	private static bool TryValidateCandidateCapacity(AuthoredResidencePlacementContract contract, CandidateDeploymentSite candidate, ref int score, ref string rejectReason, int sector, BroadLayoutLayer layer, int count, int mass, int independent, int mega, int landmarksInCell, int landmarkMega, int nearby, int altitudeSameBand, int highSkyCeiling)
	{
		if (count + 1 > MaxSitesPerSector(layer))
		{
			rejectReason = $"site-density:{sector}/{layer}:{count + 1}";
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

		if (!contract.CanShareSettlement && nearby >= contract.MaximumLocalDensity)
			score -= 280;
		if (mass > MaxSectorMass)
		{
			rejectReason = $"sector-mass:{sector}/{layer}:{mass}";
			return false;
		}

		if (layer == BroadLayoutLayer.Sky && altitudeSameBand + 1 > MaxSkySitesPerAltitudeBand)
		{
			rejectReason = $"sky-band-density:{candidate.AltitudeBand}:{altitudeSameBand + 1}";
			return false;
		}

		return true;
	}
}
