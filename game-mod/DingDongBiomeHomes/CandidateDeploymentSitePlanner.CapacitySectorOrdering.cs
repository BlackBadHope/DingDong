using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CapacitySectorSearch
	{
		private void SortOptions(int selectedIndex, CapacitySectorItem selectedItem, List<CapacitySectorOption> options)
		{
			int oceanLeftSnapshot = oceanLeft;
			int oceanRightSnapshot = oceanRight;
			Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> domainsSnapshot = activeDomains;
			int selectedLayer = (int)selectedItem.Contract.BroadLayer;
			options.Sort((left, right) =>
			{
				if (selectedItem.Contract.PreferredBiome == HomeBiome.Ocean)
				{
					bool needLeft = oceanLeftSnapshot <= oceanRightSnapshot;
					int leftSide = (left.Sector < AuthoredResidenceLayoutPolicy.MacroSectorCount / 2) == needLeft ? 0 : 1;
					int rightSide = (right.Sector < AuthoredResidenceLayoutPolicy.MacroSectorCount / 2) == needLeft ? 0 : 1;
					int side = leftSide.CompareTo(rightSide);
					if (side != 0)
						return side;
				}

				int occupancy = counts[left.Sector, selectedLayer].CompareTo(counts[right.Sector, selectedLayer]);
				if (occupancy != 0)
					return occupancy;
				int load = masses[left.Sector, selectedLayer].CompareTo(masses[right.Sector, selectedLayer]);
				if (load != 0)
					return load;
				int domain = ActiveCandidateCountInSector(selectedIndex, right.Sector, items, domainsSnapshot)
					.CompareTo(ActiveCandidateCountInSector(selectedIndex, left.Sector, items, domainsSnapshot));
				if (domain != 0)
					return domain;
				int score = right.BestScore.CompareTo(left.BestScore);
				return score != 0 ? score : left.Sector.CompareTo(right.Sector);
			});
		}
	}
}
