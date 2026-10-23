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
	private static bool HasRequiredLayerCoverage(int[, ] counts)
	{
		for (int layer = 0; layer < 4; layer++)
		{
			int total = 0;
			int used = 0;
			int left = 0;
			int right = 0;
			for (int sector = 0; sector < AuthoredResidenceLayoutPolicy.MacroSectorCount; sector++)
			{
				int value = counts[sector, layer];
				total += value;
				if (value <= 0)
					continue;
				used++;
				if (sector < AuthoredResidenceLayoutPolicy.MacroSectorCount / 2)
					left += value;
				else
					right += value;
			}

			BroadLayoutLayer broadLayer = (BroadLayoutLayer)layer;
			if (used < AuthoredResidenceLayoutPolicy.RequiredDistinctSectors(broadLayer, total))
				return false;
			// Keep the preconditioner identical to EvaluateLayerDistribution.
			if (total >= 3 && (left == 0 || right == 0))
				return false;
		}

		return true;
	}
}
