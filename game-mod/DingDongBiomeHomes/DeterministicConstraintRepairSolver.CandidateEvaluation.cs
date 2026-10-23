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
	private static (bool flowControl, bool value) EvaluateRepairCandidates<TVariable, TCandidate, TEvaluationContext>(int variableIndex, IReadOnlyList<TVariable> variables, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> aggregateFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, int maxCandidateEvaluations, RepairEvaluationBudget<TVariable> budget, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, int[, ] conflictWeights, TVariable variable, TCandidate previous, TEvaluationContext context, IReadOnlyList<TCandidate> domain, List<CandidateChoice> best, ref int bestPenalty)
		where TVariable : notnull
	{
		MutableRepairStats<TVariable> mutable = budget.Stats;
		for (int candidateIndex = 0; candidateIndex < domain.Count; candidateIndex++)
		{
			if (budget.IsExpired())
			{
				assigned.Add(variable, previous);
				return (flowControl: false, value: false);
			}

			TCandidate candidate = domain[candidateIndex];
			int penalty = 0;
			foreach (KeyValuePair<TVariable, TCandidate> other in assigned)
			{
				if (!budget.TryPair(variable, candidate, other.Key, other.Value, pairCompatible, out bool compatible))
				{
					assigned.Add(variable, previous);
					return (flowControl: false, value: false);
				}

				if (!compatible)
					penalty += conflictWeights[variableIndex, variableIndices[other.Key]];
			}

			mutable.AggregateEvaluations++;
			bool groupFeasible = aggregateFeasible(variable, candidate, context);
			if (!groupFeasible)
				penalty += variables.Count;
			int score = int.MinValue;
			if (groupFeasible && mutable.CandidateEvaluations >= maxCandidateEvaluations)
			{
				mutable.BudgetExhausted = true;
				mutable.CandidateBudgetExhausted = true;
				assigned.Add(variable, previous);
				return (flowControl: false, value: false);
			}

			if (groupFeasible)
			{
				ConstraintCandidateEvaluation candidateEvaluation = evaluate(variable, candidate, context);
				mutable.CandidateEvaluations++;
				score = candidateEvaluation.Feasible ? candidateEvaluation.Score : int.MinValue;
				if (!candidateEvaluation.Feasible)
					penalty += variables.Count;
			}

			if (penalty < bestPenalty)
			{
				bestPenalty = penalty;
				best.Clear();
				best.Add(new CandidateChoice(candidateIndex, score));
			}
			else if (penalty == bestPenalty)
			{
				best.Add(new CandidateChoice(candidateIndex, score));
			}
		}

		return (flowControl: true, value: default);
	}
}
