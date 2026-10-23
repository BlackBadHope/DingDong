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
	private static ConstraintCandidateEvaluation RejectConstraintCandidate(string npcKey, string reason, Dictionary<string, Dictionary<string, int>> rejectionReasons)
	{
		if (!rejectionReasons.TryGetValue(npcKey, out Dictionary<string, int> counts))
		{
			counts = new Dictionary<string, int>(StringComparer.Ordinal);
			rejectionReasons.Add(npcKey, counts);
		}

		counts.TryGetValue(reason, out int count);
		counts[reason] = count + 1;
		return ConstraintCandidateEvaluation.Reject;
	}
}
