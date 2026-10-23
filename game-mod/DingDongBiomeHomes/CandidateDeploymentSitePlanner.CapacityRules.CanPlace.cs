using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Candidate-envelope search shared by the independent 3F preview gate and
/// normal world generation. Independent planning intentionally ignores other
/// DingDong scenes. Simultaneous planning broad-phases with every accepted
/// protected footprint, keeps each primary authored residence focus visually
/// separate, resolves sparse-envelope overlaps against the exact mutation
/// masks, and rejects persistent-object collisions before any tile mutation.
/// </summary>
public static partial class CandidateDeploymentSitePlanner
{
	private static bool CanPlace(CapacitySectorItem item, int sector, int[, ] counts, int[, ] masses, int[, ] isolations, int[, ] megas, Dictionary<(int Sector, int Layer, HomeBiome Biome), int> landmarks)
	{
		AuthoredResidencePlacementContract contract = item.Contract;
		int layer = (int)contract.BroadLayer;
		if (counts[sector, layer] + 1 > AuthoredResidenceLayoutPolicy.MaxSitesPerSector(contract.BroadLayer))
			return false;
		if (masses[sector, layer] + contract.Mass > AuthoredResidenceLayoutPolicy.MaxSectorMass)
			return false;
		if (contract.RequiresIsolation && isolations[sector, layer] + 1 > AuthoredResidenceLayoutPolicy.MaxIndependentPerSector(contract.BroadLayer))
		{
			return false;
		}

		if (contract.ScaleClass == ResidenceScaleClass.Mega && megas[sector, layer] + 1 > AuthoredResidenceLayoutPolicy.MaxMegaPerSectorLayer)
		{
			return false;
		}

		if (contract.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega)
		{
			landmarks.TryGetValue((sector, layer, contract.PreferredBiome), out int here);
			int nextHere = here + 1;
			if (sector > 0)
			{
				landmarks.TryGetValue((sector - 1, layer, contract.PreferredBiome), out int left);
				if (left + nextHere > AuthoredResidenceLayoutPolicy.MaxAdjacentLandmarkMega)
					return false;
			}

			if (sector + 1 < AuthoredResidenceLayoutPolicy.MacroSectorCount)
			{
				landmarks.TryGetValue((sector + 1, layer, contract.PreferredBiome), out int right);
				if (right + nextHere > AuthoredResidenceLayoutPolicy.MaxAdjacentLandmarkMega)
					return false;
			}
		}

		return true;
	}
}
