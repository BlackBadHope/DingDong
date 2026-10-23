using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Small deterministic CSP solver used by the residence allocator. Variables
/// are selected with minimum-remaining-values (MRV) plus a deterministic
/// failure-weight tie break. Candidates are ordered least-constraining-first,
/// with current score as a tie break. Arc consistency removes values without
/// pairwise support, and forward checking rejects dead domains before the
/// search commits more layout state. Lazily materialized compatibility rows
/// avoid repeating provider lookups across backtracking branches.
/// </summary>
public static partial class DeterministicConstraintSolver
{
	private static bool TryVerifyLeastConstrainingOrder(ref string failure)
	{
		Dictionary<string, IReadOnlyList<int>> leastConstrainingDomains = new()
		{
			["root"] = new[]
			{
				1,
				2
			},
			["left"] = new[]
			{
				1,
				2
			},
			["right"] = new[]
			{
				1,
				2
			}
		};
		bool leastConstrainingSolved = TrySolve(new[] { "root", "left", "right" }, leastConstrainingDomains, _ => 0, (_, candidate, _) => ConstraintCandidateEvaluation.Accept(10 - candidate), (_, _, _) => true, (source, candidate, support, other) =>
		{
			bool compatible = source == "root" ? candidate == 2 || other == 1 : support == "root" ? other == 2 || candidate == 1 : true;
			return new ConstraintPairCompatibilityEvaluation(compatible, false);
		}, _ => true, false, false, 1000, 1000, 1000, 1000, 1000, out List<KeyValuePair<string, int>> leastConstrainingSolution, out ConstraintSolverStats<string> leastConstrainingStats);
		int rootValue = 0;
		foreach (KeyValuePair<string, int> assignment in leastConstrainingSolution)
		{
			if (assignment.Key == "root")
				rootValue = assignment.Value;
		}

		if (!leastConstrainingSolved || rootValue != 2 || leastConstrainingStats.ValueOrderings == 0 || leastConstrainingStats.PairChecks == 0)
		{
			failure = $"least-constraining-order:solved={leastConstrainingSolved}:root={rootValue}:orderings={leastConstrainingStats.ValueOrderings}:pairChecks={leastConstrainingStats.PairChecks}";
			return false;
		}

		return true;
	}

	private static bool TryVerifyAggregateForwardPruning(ref string failure)
	{
		Dictionary<string, IReadOnlyList<int>> aggregateDomains = new()
		{
			["anchor"] = new[]
			{
				1
			},
			["branch"] = new[]
			{
				1,
				2
			},
			["tail"] = new[]
			{
				1,
				3
			}
		};
		bool aggregateSolved = TrySolve(new[] { "anchor", "branch", "tail" }, aggregateDomains, assigned => new Dictionary<string, int>(assigned), (_, candidate, _) => ConstraintCandidateEvaluation.Accept(10 - candidate), (_, candidate, assigned) =>
		{
			foreach (int existing in assigned.Values)
			{
				if (candidate == 1 && existing == 1)
					return false;
			}

			return true;
		}, (_, _, _, _) => new(true, true), _ => true, false, true, 100, 100, 100, 1000, 100, out _, out ConstraintSolverStats<string> aggregateStats);
		if (!aggregateSolved || aggregateStats.ForwardAggregateValuesRemoved < 2 || aggregateStats.AggregateBudgetExhausted || aggregateStats.PairEvaluations != 0 || aggregateStats.PairLookups <= aggregateStats.PairEvaluations)
		{
			failure = $"aggregate-forward:solved={aggregateSolved}:removed={aggregateStats.ForwardAggregateValuesRemoved}:budget={aggregateStats.AggregateBudgetExhausted}:pairLookups={aggregateStats.PairLookups}:pairEvaluations={aggregateStats.PairEvaluations}";
			return false;
		}

		return true;
	}
}
