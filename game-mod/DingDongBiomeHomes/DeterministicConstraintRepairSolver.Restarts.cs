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
	private static void RunRepairRestarts<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> aggregateFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, int deterministicSeed, int maxRestarts, int maxStepsPerRestart, int maxCandidateEvaluations, RepairEvaluationBudget<TVariable> budget, List<IReadOnlyList<TCandidate>> orderedDomains, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, int[] selectedIndices, int[] bestSelectedIndices, ref bool hasBestAssignment, int[] conflictCounts, int[, ] conflictWeights, bool[, ] currentPairConflicts, ref bool solved, int plateauThreshold)
		where TVariable : notnull
	{
		MutableRepairStats<TVariable> mutable = budget.Stats;
		for (int restart = 0; restart < maxRestarts && !budget.IsExpired() && !mutable.BudgetExhausted; restart++)
		{
			mutable.Restarts++;
			Initialize(restart, variables, deterministicSeed, orderedDomains, assigned, selectedIndices);
			ResetConflictWeights(variables, conflictWeights);
			int previousConflictCount = int.MaxValue;
			int plateauSteps = 0;
			RunRepairSteps(variables, prepareEvaluationContext, evaluate, aggregateFeasible, pairCompatible, acceptComplete, deterministicSeed, maxStepsPerRestart, maxCandidateEvaluations, budget, orderedDomains, assigned, variableIndices, selectedIndices, bestSelectedIndices, ref hasBestAssignment, conflictCounts, conflictWeights, currentPairConflicts, ref solved, plateauThreshold, restart, ref previousConflictCount, ref plateauSteps);
			if (solved)
				break;
		}
	}
}
