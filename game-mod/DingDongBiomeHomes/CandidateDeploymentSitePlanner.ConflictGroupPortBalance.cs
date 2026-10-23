using System.Collections.Generic;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConflictGroupRepairSearch
	{
		private List<RankedLayoutCandidate> BalancePortCandidates(string variable, List<RankedLayoutCandidate> viable)
		{
			List<RankedLayoutCandidate> left = new();
			List<RankedLayoutCandidate> right = new();
			RankedLayoutCandidate current = working[variable];
			foreach (RankedLayoutCandidate candidate in viable)
			{
				if (EqualityComparer<RankedLayoutCandidate>.Default.Equals(candidate, current))
					continue;
				if (candidate.Site.AssetArea.Center.X < Main.maxTilesX / 2)
					left.Add(candidate);
				else
					right.Add(candidate);
			}

			List<RankedLayoutCandidate> balanced = new(ConstraintHardSafetyPortCohortDomainLimit);
			HashSet<(int Side, int Bucket)> seenBuckets = new();
			if (viable.Contains(current))
				AddSpatiallyDistinct(current, balanced, seenBuckets);
			for (int index = 0; balanced.Count < ConstraintHardSafetyPortCohortDomainLimit && (index < left.Count || index < right.Count); index++)
			{
				if (index < left.Count)
					AddSpatiallyDistinct(left[index], balanced, seenBuckets);
				if (index < right.Count)
					AddSpatiallyDistinct(right[index], balanced, seenBuckets);
			}
			return balanced.Count > 0 ? balanced : viable;
		}

		private bool TrySearchDomains(List<string> movingVariables, Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> domains)
		{
			Dictionary<string, RankedLayoutCandidate> assigned = new(System.StringComparer.Ordinal);
			Dictionary<string, RankedLayoutCandidate> original = new(System.StringComparer.Ordinal);
			foreach (string variable in movingVariables)
				original.Add(variable, working[variable]);
			Dictionary<string, List<RankedLayoutCandidate>> initialDomains = new(System.StringComparer.Ordinal);
			foreach (KeyValuePair<string, IReadOnlyList<RankedLayoutCandidate>> domain in domains)
				initialDomains.Add(domain.Key, new List<RankedLayoutCandidate>(domain.Value));
			return SearchHardSafetyConflictGroup(initialDomains, masks, variables, candidateIds, hardPairCompatibilityCache, working, baselineConflicts, ref localPairChecks, ref budgetExhausted, ref localNodes, assigned, original);
		}
	}
}
