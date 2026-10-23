using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private readonly record struct CapacityWitnessStats(
		int NodesVisited,
		long PairLookups,
		int PairEvaluations,
		int MaxAssigned,
		bool LeafBudgetExhausted,
		string FailedLayer);

	private static bool TryFindPairCompatibleAssignment(
		IReadOnlyList<string> variables,
		IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> domains,
		Dictionary<string, AuthoredSceneMutationMask> masks,
		Func<string, RankedLayoutCandidate, string, RankedLayoutCandidate,
			ConstraintPairCompatibilityEvaluation> pairCompatible,
		Func<IReadOnlyDictionary<string, RankedLayoutCandidate>, bool> acceptComplete,
		out List<KeyValuePair<string, RankedLayoutCandidate>> solution,
		out CapacityWitnessStats witnessStats)
	{
		Dictionary<string, RankedLayoutCandidate> merged = new(StringComparer.Ordinal);
		int nodes = 0;
		long pairLookups = 0;
		int pairEvaluations = 0;
		int maxAssigned = 0;
		bool leafBudgetExhausted = false;

		for (int layerIndex = 0; layerIndex < 4; layerIndex++)
		{
			BroadLayoutLayer layer = (BroadLayoutLayer)layerIndex;
			List<string> layerVariables = VariablesForLayer(variables, layer);
			if (layerVariables.Count == 0)
				continue;
			Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> layerDomains =
				DomainsForLayer(layerVariables, domains);
			bool solved = DeterministicConstraintSolver.TrySolve(layerVariables,
				layerDomains, static assigned => assigned.Count,
				static (_, candidate, _) => ConstraintCandidateEvaluation.Accept(candidate.BaseScore),
				static (_, _, _) => true, pairCompatible,
				assigned => AcceptLayerHardSafety(assigned, masks, layerVariables),
				enforceInitialArcConsistency: false,
				enforceForwardFeasibility: false,
				ConstraintSectorDownstreamPairEvaluationLimit,
				ConstraintSectorDownstreamPairEvaluationLimit,
				ConstraintSectorDownstreamCandidateEvaluationLimit,
				ConstraintSectorDownstreamTimeLimitMilliseconds,
				ConstraintSectorDownstreamNodeLimit,
				out List<KeyValuePair<string, RankedLayoutCandidate>> layerSolution,
				out ConstraintSolverStats<string> stats);
			AccumulateWitnessStats(stats, merged.Count, ref nodes, ref pairLookups,
				ref pairEvaluations, ref maxAssigned, ref leafBudgetExhausted);
			if (!solved)
			{
				solution = new List<KeyValuePair<string, RankedLayoutCandidate>>();
				witnessStats = new CapacityWitnessStats(nodes, pairLookups,
					pairEvaluations, maxAssigned, leafBudgetExhausted, layer.ToString());
				return false;
			}
			foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in layerSolution)
				merged.Add(assignment.Key, assignment.Value);
			maxAssigned = Math.Max(maxAssigned, merged.Count);
		}

		if (!CrossLayerPairsAccept(variables, merged, pairCompatible,
			ref pairLookups, ref pairEvaluations) || !acceptComplete(merged))
		{
			solution = new List<KeyValuePair<string, RankedLayoutCandidate>>();
			witnessStats = new CapacityWitnessStats(nodes, pairLookups,
				pairEvaluations, maxAssigned, leafBudgetExhausted, "Complete");
			return false;
		}

		solution = new List<KeyValuePair<string, RankedLayoutCandidate>>(variables.Count);
		foreach (string variable in variables)
			solution.Add(new KeyValuePair<string, RankedLayoutCandidate>(variable, merged[variable]));
		witnessStats = new CapacityWitnessStats(nodes, pairLookups,
			pairEvaluations, maxAssigned, leafBudgetExhausted, "none");
		return true;
	}

}
