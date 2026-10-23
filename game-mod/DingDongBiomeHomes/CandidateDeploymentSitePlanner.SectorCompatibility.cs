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
	private static bool CompatibleWithAssigned(int itemIndex, int sector, Func<string, RankedLayoutCandidate, string, RankedLayoutCandidate, bool> pairCompatible, List<CapacitySectorItem> items, Dictionary<(int ItemIndex, int Sector), IReadOnlyList<RankedLayoutCandidate>> candidatesByItemSector, Dictionary<string, int> assignedSectors, bool[] assigned, Dictionary<(int LeftIndex, int LeftSector, int RightIndex, int RightSector), bool> sectorPairSupportCache, ref int sectorPairSupportQueries, ref int sectorPairSupportCacheHits, ref int sectorPairSupportCandidateEvaluations, ref bool sectorPairSupportBudgetExhausted)
	{
		for (int otherIndex = 0; otherIndex < items.Count; otherIndex++)
		{
			if (!assigned[otherIndex])
				continue;
			if (!HasSectorPairSupport(itemIndex, sector, otherIndex, assignedSectors[items[otherIndex].Variable], pairCompatible, items, candidatesByItemSector, sectorPairSupportCache, ref sectorPairSupportQueries, ref sectorPairSupportCacheHits, ref sectorPairSupportCandidateEvaluations, ref sectorPairSupportBudgetExhausted))
				return false;
		}

		return true;
	}

	private static bool HasSectorPairSupport(int leftIndex, int leftSector, int rightIndex, int rightSector, Func<string, RankedLayoutCandidate, string, RankedLayoutCandidate, bool> pairCompatible, List<CapacitySectorItem> items, Dictionary<(int ItemIndex, int Sector), IReadOnlyList<RankedLayoutCandidate>> candidatesByItemSector, Dictionary<(int LeftIndex, int LeftSector, int RightIndex, int RightSector), bool> sectorPairSupportCache, ref int sectorPairSupportQueries, ref int sectorPairSupportCacheHits, ref int sectorPairSupportCandidateEvaluations, ref bool sectorPairSupportBudgetExhausted)
	{
		sectorPairSupportQueries++;
		if (leftIndex > rightIndex)
		{
			(leftIndex, rightIndex) = (rightIndex, leftIndex);
			(leftSector, rightSector) = (rightSector, leftSector);
		}

		var key = (leftIndex, leftSector, rightIndex, rightSector);
		if (sectorPairSupportCache.TryGetValue(key, out bool cached))
		{
			sectorPairSupportCacheHits++;
			return cached;
		}

		AuthoredResidencePlacementContract leftContract = items[leftIndex].Contract;
		AuthoredResidencePlacementContract rightContract = items[rightIndex].Contract;
		int sectorDistance = Math.Abs(leftSector - rightSector);
		bool hallowMegaPair = leftContract.ScaleClass == ResidenceScaleClass.Mega && rightContract.ScaleClass == ResidenceScaleClass.Mega && leftContract.MacroWindow == ResidenceMacroWindow.HallowEitherShelf && rightContract.MacroWindow == ResidenceMacroWindow.HallowEitherShelf;
		if (leftContract.BroadLayer != rightContract.BroadLayer || sectorDistance > (hallowMegaPair ? 2 : 1))
		{
			sectorPairSupportCache.Add(key, true);
			return true;
		}

		if (sectorPairSupportBudgetExhausted)
			return true;
		IReadOnlyList<RankedLayoutCandidate> leftCandidates = candidatesByItemSector[(leftIndex, leftSector)];
		IReadOnlyList<RankedLayoutCandidate> rightCandidates = candidatesByItemSector[(rightIndex, rightSector)];
		foreach (RankedLayoutCandidate leftCandidate in leftCandidates)
		{
			foreach (RankedLayoutCandidate rightCandidate in rightCandidates)
			{
				if (sectorPairSupportCandidateEvaluations >= ConstraintSectorSupportPairEvaluationLimit)
				{
					// An incomplete support search cannot soundly reject the option.
					sectorPairSupportBudgetExhausted = true;
					return true;
				}

				sectorPairSupportCandidateEvaluations++;
				if (!pairCompatible(items[leftIndex].Variable, leftCandidate, items[rightIndex].Variable, rightCandidate))
				{
					continue;
				}

				sectorPairSupportCache.Add(key, true);
				return true;
			}
		}

		sectorPairSupportCache.Add(key, false);
		return false;
	}
}
