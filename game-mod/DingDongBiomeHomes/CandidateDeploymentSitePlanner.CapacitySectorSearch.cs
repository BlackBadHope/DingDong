using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CapacitySectorSearch
	{
		private bool Search(int depth)
		{
			if (depth == items.Count)
				return TryAcceptLeaf();
			if (!TrySelectNext(out int selectedIndex, out List<CapacitySectorOption> options))
				return false;

			CapacitySectorItem selectedItem = items[selectedIndex];
			SortOptions(selectedIndex, selectedItem, options);
			foreach (CapacitySectorOption option in options)
			{
				if (nodes >= ConstraintSectorAllocationNodeLimit)
				{
					budgetExhausted = true;
					return false;
				}
				nodes++;
				BeginAssignment(depth, selectedIndex, selectedItem, option);
				Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> previousDomains = activeDomains;
				bool previousArcState = activeDomainsArcConsistent;
				bool branchConsistent = TryPrepareBranch(selectedIndex, selectedItem, option);
				if (branchConsistent && Search(depth + 1))
					return true;
				activeDomains = previousDomains;
				activeDomainsArcConsistent = previousArcState;
				EndAssignment(selectedIndex, selectedItem, option);
				backtracks++;
				if (budgetExhausted)
					return false;
			}
			return false;
		}

		private bool TrySelectNext(out int selectedIndex, out List<CapacitySectorOption> selectedOptions)
		{
			selectedIndex = -1;
			selectedOptions = null;
			for (int i = 0; i < items.Count; i++)
			{
				if (assigned[i])
					continue;
				CapacitySectorItem item = items[i];
				List<CapacitySectorOption> feasible = FeasibleOptions(i, item);
				if (feasible.Count == 0)
				{
					deadDomains++;
					return false;
				}
				if (selectedOptions == null || feasible.Count < selectedOptions.Count || feasible.Count == selectedOptions.Count && CompareCapacityPriority(item, items[selectedIndex]) < 0)
				{
					selectedIndex = i;
					selectedOptions = feasible;
				}
			}
			return true;
		}

		private List<CapacitySectorOption> FeasibleOptions(int itemIndex, CapacitySectorItem item)
		{
			List<CapacitySectorOption> feasible = new();
			foreach (CapacitySectorOption option in item.Options)
			{
				if (!CanPlace(item, option.Sector, counts, masses, isolations, megas, landmarks) || !HasActiveCandidateInSector(itemIndex, option.Sector, items, activeDomains))
					continue;
				if (!CompatibleWithAssigned(itemIndex, option.Sector, pairCompatible, items, candidatesByItemSector, assignedSectors, assigned, sectorPairSupportCache, ref sectorPairSupportQueries, ref sectorPairSupportCacheHits, ref sectorPairSupportCandidateEvaluations, ref sectorPairSupportBudgetExhausted))
				{
					sectorPairRejectedOptions++;
					continue;
				}
				feasible.Add(option);
			}
			return feasible;
		}
	}
}
