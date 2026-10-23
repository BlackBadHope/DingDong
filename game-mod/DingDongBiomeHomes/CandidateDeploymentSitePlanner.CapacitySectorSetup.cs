using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CapacitySectorSearch
	{
		public bool TryRun(out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> restrictedPools, out string summary, out string failure)
		{
			if (!TryBuildItems(out string failedVariable))
			{
				restrictedPools = new Dictionary<string, IReadOnlyList<RankedLayoutCandidate>>(StringComparer.Ordinal);
				summary = $"sectorItems={items.Count}/{variables.Count}|sectorOptions={optionTotal}";
				failure = $"sector-domain-empty:{failedVariable}";
				return false;
			}
			BuildCandidateIndex();
			bool solved = Search(depth: 0);
			if (!solved)
			{
				restrictedPools = new Dictionary<string, IReadOnlyList<RankedLayoutCandidate>>(StringComparer.Ordinal);
				summary = FailureSummary();
				failure = downstreamAdmissionBudgetExhausted
					? "sector-downstream-budget-exhausted"
					: budgetExhausted ? "sector-node-budget-exhausted" : "capacity-sector-layout-not-found";
				return false;
			}

			restrictedPools = acceptedRestrictedPools;
			summary = SuccessSummary(restrictedPools);
			failure = string.Empty;
			return true;
		}

		private bool TryBuildItems(out string failedVariable)
		{
			foreach (string variable in variables)
			{
				AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(variable);
				Dictionary<int, int> bestBySector = BestScoresBySector(pools[variable]);
				if (contract.PreferredBiome == HomeBiome.Ocean)
				{
					oceanTotal++;
					foreach (int sector in bestBySector.Keys)
					{
						if (sector < AuthoredResidenceLayoutPolicy.MacroSectorCount / 2)
							oceanSupportsLeft = true;
						else
							oceanSupportsRight = true;
					}
				}
				List<CapacitySectorOption> options = BuildSectorOptions(bestBySector);
				if (options.Count == 0)
				{
					failedVariable = variable;
					return false;
				}
				items.Add(new CapacitySectorItem(variable, contract, options));
				optionTotal += options.Count;
				optionMinimum = Math.Min(optionMinimum, options.Count);
				optionMaximum = Math.Max(optionMaximum, options.Count);
			}
			failedVariable = string.Empty;
			return true;
		}

		private static Dictionary<int, int> BestScoresBySector(IReadOnlyList<RankedLayoutCandidate> candidates)
		{
			Dictionary<int, int> bestBySector = new();
			foreach (RankedLayoutCandidate candidate in candidates)
			{
				int sector = AuthoredResidenceLayoutPolicy.SectorOf(candidate.Site.AssetArea.Center.X);
				if (!bestBySector.TryGetValue(sector, out int best) || candidate.BaseScore > best)
					bestBySector[sector] = candidate.BaseScore;
			}
			return bestBySector;
		}

		private static List<CapacitySectorOption> BuildSectorOptions(Dictionary<int, int> bestBySector)
		{
			List<CapacitySectorOption> options = new(bestBySector.Count);
			foreach (KeyValuePair<int, int> option in bestBySector)
				options.Add(new CapacitySectorOption(option.Key, option.Value));
			options.Sort(static (left, right) =>
			{
				int score = right.BestScore.CompareTo(left.BestScore);
				return score != 0 ? score : left.Sector.CompareTo(right.Sector);
			});
			return options;
		}

		private void BuildCandidateIndex()
		{
			for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
			{
				CapacitySectorItem item = items[itemIndex];
				foreach (CapacitySectorOption option in item.Options)
				{
					List<RankedLayoutCandidate> candidates = new();
					foreach (RankedLayoutCandidate candidate in pools[item.Variable])
					{
						if (AuthoredResidenceLayoutPolicy.SectorOf(candidate.Site.AssetArea.Center.X) == option.Sector)
							candidates.Add(candidate);
					}
					candidatesByItemSector.Add((itemIndex, option.Sector), candidates);
				}
			}
		}
	}
}
