using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private static List<string> VariablesForLayer(IReadOnlyList<string> variables,
		BroadLayoutLayer layer)
	{
		List<string> result = new();
		foreach (string variable in variables)
		{
			if (AuthoredResidencePlacementManifest.Required(variable).BroadLayer == layer)
				result.Add(variable);
		}
		return result;
	}

	private static Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> DomainsForLayer(
		IReadOnlyList<string> variables,
		IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> domains)
	{
		Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> result =
			new(StringComparer.Ordinal);
		foreach (string variable in variables)
			result.Add(variable, domains[variable]);
		return result;
	}

	private static void AccumulateWitnessStats(ConstraintSolverStats<string> stats,
		int alreadyAssigned, ref int nodes, ref long pairLookups,
		ref int pairEvaluations, ref int maxAssigned, ref bool leafBudgetExhausted)
	{
		nodes += stats.NodesVisited;
		pairLookups += stats.PairLookups + stats.ArcPairLookups;
		pairEvaluations += stats.PairEvaluations + stats.ArcPairEvaluations;
		maxAssigned = Math.Max(maxAssigned, alreadyAssigned + stats.MaxAssigned);
		leafBudgetExhausted |= stats.BudgetExhausted || stats.PairBudgetExhausted ||
			stats.TimeBudgetExhausted;
	}
}
