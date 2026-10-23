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
	private static void AccumulateArcStats(ArcConsistencyPruneStats<string> arcStats, ref int arcPairEvaluations, ref int arcRevisions, ref int arcValuesRemoved, ref bool arcBudgetExhausted)
	{
		arcPairEvaluations += arcStats.PairEvaluations;
		arcRevisions += arcStats.Revisions;
		arcValuesRemoved += arcStats.ValuesRemoved;
		arcBudgetExhausted |= arcStats.BudgetExhausted;
	}

	private static bool TryRestrictActiveDomain(int itemIndex, int sector, out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> restrictedDomains, List<CapacitySectorItem> items, Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> activeDomains)
	{
		CapacitySectorItem item = items[itemIndex];
		List<RankedLayoutCandidate> restricted = new();
		foreach (RankedLayoutCandidate candidate in activeDomains[item.Variable])
		{
			if (AuthoredResidenceLayoutPolicy.SectorOf(candidate.Site.AssetArea.Center.X) == sector)
				restricted.Add(candidate);
		}

		restrictedDomains = new Dictionary<string, IReadOnlyList<RankedLayoutCandidate>>(activeDomains, StringComparer.Ordinal)
		{
			[item.Variable] = restricted
		};
		return restricted.Count > 0;
	}

	private static bool HasActiveCandidateInSector(int itemIndex, int sector, List<CapacitySectorItem> items, Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> activeDomains)
	{
		return ActiveCandidateCountInSector(itemIndex, sector, items, activeDomains) > 0;
	}

	private static int ActiveCandidateCountInSector(int itemIndex, int sector, List<CapacitySectorItem> items, Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> activeDomains)
	{
		int count = 0;
		foreach (RankedLayoutCandidate candidate in activeDomains[items[itemIndex].Variable])
		{
			if (AuthoredResidenceLayoutPolicy.SectorOf(candidate.Site.AssetArea.Center.X) == sector)
				count++;
		}

		return count;
	}

	private static Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> SnapshotActiveDomains(Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> activeDomains)
	{
		return new Dictionary<string, IReadOnlyList<RankedLayoutCandidate>>(activeDomains, StringComparer.Ordinal);
	}

	private static int CompareCapacityPriority(CapacitySectorItem left, CapacitySectorItem right)
	{
		int mega = (right.Contract.ScaleClass == ResidenceScaleClass.Mega).CompareTo(left.Contract.ScaleClass == ResidenceScaleClass.Mega);
		if (mega != 0)
			return mega;
		int isolation = right.Contract.RequiresIsolation.CompareTo(left.Contract.RequiresIsolation);
		if (isolation != 0)
			return isolation;
		int mass = right.Contract.Mass.CompareTo(left.Contract.Mass);
		return mass != 0 ? mass : string.Compare(left.Variable, right.Variable, StringComparison.Ordinal);
	}
}
