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
	private static bool RepairVariable<TVariable, TCandidate, TEvaluationContext>(int variableIndex, int restart, int step, bool forceMove, IReadOnlyList<TVariable> variables, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> aggregateFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, int deterministicSeed, int maxCandidateEvaluations, RepairEvaluationBudget<TVariable> budget, List<IReadOnlyList<TCandidate>> orderedDomains, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, int[] selectedIndices, int[] conflictCounts, int[, ] conflictWeights)
		where TVariable : notnull
	{
		MutableRepairStats<TVariable> mutable = budget.Stats;
		if (budget.IsExpired())
			return false;
		TVariable variable = variables[variableIndex];
		TCandidate previous = assigned[variable];
		int previousIndex = selectedIndices[variableIndex];
		assigned.Remove(variable);
		TEvaluationContext context = prepareEvaluationContext(assigned);
		IReadOnlyList<TCandidate> domain = orderedDomains[variableIndex];
		List<CandidateChoice> best = new();
		int bestPenalty = int.MaxValue;
		(bool flowControl, bool value) = EvaluateRepairCandidates(variableIndex, variables, evaluate, aggregateFeasible, pairCompatible, maxCandidateEvaluations, budget, assigned, variableIndices, conflictWeights, variable, previous, context, domain, best, ref bestPenalty);
		if (!flowControl)
		{
			return value;
		}

		bool avoidCurrent = forceMove || conflictCounts[variableIndex] > 0;
		int preferredScore = int.MinValue;
		foreach (CandidateChoice choice in best)
		{
			if (avoidCurrent && best.Count > 1 && choice.Index == previousIndex)
				continue;
			if (choice.Score > preferredScore)
				preferredScore = choice.Score;
		}

		List<int> finalists = new();
		foreach (CandidateChoice choice in best)
		{
			if (avoidCurrent && best.Count > 1 && choice.Index == previousIndex)
				continue;
			if (choice.Score == preferredScore)
				finalists.Add(choice.Index);
		}

		if (finalists.Count == 0)
			finalists.Add(previousIndex);
		int tie = PositiveModulo(deterministicSeed + restart * 193 + step * 43 + variableIndex * 17, finalists.Count);
		int chosenIndex = finalists[tie];
		selectedIndices[variableIndex] = chosenIndex;
		assigned.Add(variable, domain[chosenIndex]);
		mutable.Repairs++;
		if (chosenIndex == previousIndex)
			mutable.SidewaysRepairs++;
		return true;
	}
}
