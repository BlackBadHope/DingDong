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
	private static int AnalyzeConflicts<TVariable, TCandidate, TEvaluationContext>(out int weightedTotal, IReadOnlyList<TVariable> variables, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, bool> aggregateFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, RepairEvaluationBudget<TVariable> budget, Dictionary<TVariable, TCandidate> assigned, int[] conflictCounts, int[, ] conflictWeights, bool[, ] currentPairConflicts)
		where TVariable : notnull
	{
		MutableRepairStats<TVariable> mutable = budget.Stats;
		Array.Clear(conflictCounts, 0, conflictCounts.Length);
		Array.Clear(currentPairConflicts, 0, currentPairConflicts.Length);
		int total = 0;
		weightedTotal = 0;
		for (int i = 0; i < variables.Count; i++)
		{
			if (budget.IsExpired())
				return total;
			for (int j = i + 1; j < variables.Count; j++)
			{
				if (!budget.TryPair(variables[i], assigned[variables[i]], variables[j], assigned[variables[j]], pairCompatible, out bool compatible))
					return total;
				if (compatible)
					continue;
				int weight = conflictWeights[i, j];
				currentPairConflicts[i, j] = true;
				currentPairConflicts[j, i] = true;
				conflictCounts[i] += weight;
				conflictCounts[j] += weight;
				total++;
				weightedTotal += weight;
			}
		}

		for (int i = 0; i < variables.Count; i++)
		{
			if (budget.IsExpired())
				return total;
			TVariable variable = variables[i];
			TCandidate candidate = assigned[variable];
			assigned.Remove(variable);
			TEvaluationContext context = prepareEvaluationContext(assigned);
			bool feasible = aggregateFeasible(variable, candidate, context);
			assigned.Add(variable, candidate);
			mutable.AggregateEvaluations++;
			if (feasible)
				continue;
			conflictCounts[i] += variables.Count;
			total++;
			weightedTotal += variables.Count;
		}

		return total;
	}
}
