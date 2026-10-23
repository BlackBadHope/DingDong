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
	private static string ConstraintRejectionSummary(string npcKey, IReadOnlyDictionary<string, Dictionary<string, int>> rejectionReasons)
	{
		if (string.Equals(npcKey, "none", StringComparison.Ordinal) || !rejectionReasons.TryGetValue(npcKey, out Dictionary<string, int> counts) || counts.Count == 0)
		{
			return "none";
		}

		List<KeyValuePair<string, int>> ordered = new(counts);
		ordered.Sort(static (left, right) =>
		{
			int count = right.Value.CompareTo(left.Value);
			return count != 0 ? count : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
		});
		int take = Math.Min(6, ordered.Count);
		List<string> parts = new(take);
		for (int i = 0; i < take; i++)
			parts.Add($"{ordered[i].Key}:{ordered[i].Value}");
		return string.Join(',', parts);
	}
}
