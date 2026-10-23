using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Deterministic bounded min-conflicts solver. Unlike chronological
/// backtracking it always owns a complete assignment and can repair any
/// conflicted variable, which is a better fit for large, broad spatial domains.
/// The caller's complete validator remains the final acceptance oracle. When
/// no solution is found, the output still contains the best complete assignment
/// observed so an exact solver can use it as a soft value-order hint.
/// </summary>
public static partial class DeterministicConstraintRepairSolver
{
	public static bool TrySelfTest(out string failure)
	{
		string[] variables =
		{
			"a",
			"b",
			"c"
		};
		Dictionary<string, IReadOnlyList<int>> domains = new()
		{
			["a"] = new[]
			{
				1,
				2,
				3
			},
			["b"] = new[]
			{
				1,
				2,
				3
			},
			["c"] = new[]
			{
				1,
				2,
				3
			}
		};
		bool solved = TrySolve(variables, domains, assigned => new Dictionary<string, int>(assigned), (_, candidate, _) => ConstraintCandidateEvaluation.Accept(10 - candidate), (_, _, _) => true, (_, candidate, _, other) => new ConstraintPairCompatibilityEvaluation(candidate != other, false), assigned => assigned.Count == 3, 17, 4, 100, 10000, 1000, 1000, out List<KeyValuePair<string, int>> solution, out ConstraintRepairStats<string> stats);
		if (!solved || solution.Count != 3 || stats.BudgetExhausted || stats.Repairs == 0)
		{
			failure = $"repair:nodes={stats.Steps}:repairs={stats.Repairs}:best={stats.BestConflictCount}:budget={stats.BudgetExhausted}";
			return false;
		}

		HashSet<int> unique = new();
		foreach (KeyValuePair<string, int> assignment in solution)
			unique.Add(assignment.Value);
		if (unique.Count != 3)
		{
			failure = $"repair:unique={unique.Count}/3";
			return false;
		}

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
		bool unsatisfiedSolved = TrySolve(new[] { "left", "right" }, unsatisfiedDomains, assigned => new Dictionary<string, int>(assigned), (_, _, _) => ConstraintCandidateEvaluation.Accept(0), (_, _, _) => true, (_, _, _, _) => new ConstraintPairCompatibilityEvaluation(false, false), _ => true, 23, 1, 1, 100, 100, 1000, out List<KeyValuePair<string, int>> bestAssignment, out ConstraintRepairStats<string> unsatisfiedStats);
		if (unsatisfiedSolved || bestAssignment.Count != 2 || unsatisfiedStats.BestConflictCount != 1)
		{
			failure = $"repair-best-handoff:solved={unsatisfiedSolved}:assignment={bestAssignment.Count}/2:best={unsatisfiedStats.BestConflictCount}";
			return false;
		}

		bool watchdogSolved = TrySolve(new[] { "slow-left", "slow-right" }, new Dictionary<string, IReadOnlyList<int>> { ["slow-left"] = new[] { 1 }, ["slow-right"] = new[] { 1 } }, assigned => new Dictionary<string, int>(assigned), (_, _, _) => ConstraintCandidateEvaluation.Accept(0), (_, _, _) => true, (_, _, _, _) =>
		{
			System.Threading.Thread.Sleep(20);
			return new ConstraintPairCompatibilityEvaluation(false, true);
		}, _ => true, 29, 1, 1, 100, 100, 1, out List<KeyValuePair<string, int>> watchdogAssignment, out ConstraintRepairStats<string> watchdogStats);
		if (watchdogSolved || !watchdogStats.BudgetExhausted || !watchdogStats.TimeBudgetExhausted || watchdogStats.ElapsedMilliseconds < 1)
		{
			failure = $"repair-watchdog:solved={watchdogSolved}:assignment={watchdogAssignment.Count}:budget={watchdogStats.BudgetExhausted}:timeBudget={watchdogStats.TimeBudgetExhausted}:elapsedMs={watchdogStats.ElapsedMilliseconds}";
			return false;
		}

		failure = string.Empty;
		return true;
	}
}
