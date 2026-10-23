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
	private static bool TryVerifyArcWipeoutDiagnostics(ref string failure)
	{
		Dictionary<string, IReadOnlyList<int>> unsatisfiedDomains = new()
		{
			["left"] = new[]
			{
				1
			},
			["right"] = new[]
			{
				1
			}
		};
		bool unsatisfiedSolved = TrySolve(new[] { "left", "right" }, unsatisfiedDomains, _ => 0, (_, _, _) => ConstraintCandidateEvaluation.Accept(0), (_, _, _) => true, (_, candidate, _, other) => new(candidate != other, false), _ => true, true, false, 10, 10, 10, 1000, 10, out _, out ConstraintSolverStats<string> unsatisfiedStats);
		if (unsatisfiedSolved || !unsatisfiedStats.ArcWipeout || unsatisfiedStats.ArcBlockedVariable == null || unsatisfiedStats.ArcWitnessVariable == null || unsatisfiedStats.ConstraintWeightUpdates == 0 || unsatisfiedStats.MaxConstraintWeight < 2)
		{
			failure = $"arc-wipeout:solved={unsatisfiedSolved}:wipeout={unsatisfiedStats.ArcWipeout}:blocked={unsatisfiedStats.ArcBlockedVariable}:witness={unsatisfiedStats.ArcWitnessVariable}:weightUpdates={unsatisfiedStats.ConstraintWeightUpdates}:maxWeight={unsatisfiedStats.MaxConstraintWeight}";
			return false;
		}

		return true;
	}

	private static bool TryVerifyArcMrvForwardCheck(ref string failure)
	{
		string[] variables =
		{
			"wide",
			"scarce",
			"tail"
		};
		Dictionary<string, IReadOnlyList<int>> domains = new()
		{
			["wide"] = new[]
			{
				1,
				2
			},
			["scarce"] = new[]
			{
				1
			},
			["tail"] = new[]
			{
				2,
				3
			}
		};
		bool solved = TrySolve(variables, domains, _ => 0, (_, candidate, _) => ConstraintCandidateEvaluation.Accept(10 - candidate), (_, _, _) => true, (_, candidate, _, other) => new(candidate != other, false), _ => true, true, false, 100, 100, 100, 1000, 100, out List<KeyValuePair<string, int>> solution, out ConstraintSolverStats<string> stats);
		if (!solved || solution.Count != 3 || stats.BudgetExhausted || stats.ArcBudgetExhausted || stats.ArcValuesRemoved < 2)
		{
			failure = $"arc-mrv-forward-check:nodes={stats.NodesVisited}:assigned={stats.MaxAssigned}:searchBudget={stats.BudgetExhausted}:arcBudget={stats.ArcBudgetExhausted}:arcRemoved={stats.ArcValuesRemoved}";
			return false;
		}

		foreach (KeyValuePair<string, int> assignment in solution)
		{
			if (assignment.Key == "scarce" && assignment.Value != 1 || assignment.Key == "wide" && assignment.Value != 2 || assignment.Key == "tail" && assignment.Value != 3)
			{
				failure = $"mrv-forward-check:unexpected={assignment.Key}/{assignment.Value}";
				return false;
			}
		}

		return true;
	}
}
