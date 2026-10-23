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
	private static void Place(CapacitySectorItem item, int sector, int delta, int[, ] counts, int[, ] masses, int[, ] isolations, int[, ] megas, Dictionary<(int Sector, int Layer, HomeBiome Biome), int> landmarks, ref int oceanLeft, ref int oceanRight)
	{
		AuthoredResidencePlacementContract contract = item.Contract;
		int layer = (int)contract.BroadLayer;
		counts[sector, layer] += delta;
		masses[sector, layer] += contract.Mass * delta;
		if (contract.RequiresIsolation)
			isolations[sector, layer] += delta;
		if (contract.ScaleClass == ResidenceScaleClass.Mega)
			megas[sector, layer] += delta;
		if (contract.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega)
		{
			var key = (sector, layer, contract.PreferredBiome);
			landmarks.TryGetValue(key, out int count);
			int next = count + delta;
			if (next == 0)
				landmarks.Remove(key);
			else
				landmarks[key] = next;
		}

		if (contract.PreferredBiome == HomeBiome.Ocean)
		{
			if (sector < AuthoredResidenceLayoutPolicy.MacroSectorCount / 2)
				oceanLeft += delta;
			else
				oceanRight += delta;
		}
	}
}
