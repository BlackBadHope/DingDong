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
	private static bool CanStillMeetRequiredLayerCoverage(List<CapacitySectorItem> items, int[, ] counts, int[, ] masses, int[, ] isolations, int[, ] megas, Dictionary<(int Sector, int Layer, HomeBiome Biome), int> landmarks, bool[] assigned, Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> activeDomains)
	{
		for (int layer = 0; layer < 4; layer++)
		{
			bool[] possibleSectors = new bool[AuthoredResidenceLayoutPolicy.MacroSectorCount];
			int total = 0;
			bool possibleLeft = false;
			bool possibleRight = false;
			for (int sector = 0; sector < AuthoredResidenceLayoutPolicy.MacroSectorCount; sector++)
			{
				int value = counts[sector, layer];
				total += value;
				if (value <= 0)
					continue;
				possibleSectors[sector] = true;
				if (sector < AuthoredResidenceLayoutPolicy.MacroSectorCount / 2)
					possibleLeft = true;
				else
					possibleRight = true;
			}

			for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
			{
				if (assigned[itemIndex] || (int)items[itemIndex].Contract.BroadLayer != layer)
					continue;
				total++;
				bool hasOption = false;
				foreach (CapacitySectorOption option in items[itemIndex].Options)
				{
					if (!CanPlace(items[itemIndex], option.Sector, counts, masses, isolations, megas, landmarks) || !HasActiveCandidateInSector(itemIndex, option.Sector, items, activeDomains))
					{
						continue;
					}

					hasOption = true;
					possibleSectors[option.Sector] = true;
					if (option.Sector < AuthoredResidenceLayoutPolicy.MacroSectorCount / 2)
						possibleLeft = true;
					else
						possibleRight = true;
				}

				if (!hasOption)
					return false;
			}

			int possibleUsed = 0;
			foreach (bool possible in possibleSectors)
			{
				if (possible)
					possibleUsed++;
			}

			BroadLayoutLayer broadLayer = (BroadLayoutLayer)layer;
			if (possibleUsed < AuthoredResidenceLayoutPolicy.RequiredDistinctSectors(broadLayer, total))
				return false;
			// Match final layout acceptance: two sites must occupy distinct sectors,
			// but only three or more make cross-world balance meaningful.
			if (total >= 3 && (!possibleLeft || !possibleRight))
				return false;
		}

		return true;
	}
}
