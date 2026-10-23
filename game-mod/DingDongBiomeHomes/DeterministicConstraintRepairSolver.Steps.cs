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
	private static void RunRepairSteps<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> aggregateFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, int deterministicSeed, int maxStepsPerRestart, int maxCandidateEvaluations, RepairEvaluationBudget<TVariable> budget, List<IReadOnlyList<TCandidate>> orderedDomains, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, int[] selectedIndices, int[] bestSelectedIndices, ref bool hasBestAssignment, int[] conflictCounts, int[, ] conflictWeights, bool[, ] currentPairConflicts, ref bool solved, int plateauThreshold, int restart, ref int previousConflictCount, ref int plateauSteps)
		where TVariable : notnull
	{
		MutableRepairStats<TVariable> mutable = budget.Stats;
		for (int step = 0; step < maxStepsPerRestart && !budget.IsExpired() && !mutable.BudgetExhausted; step++)
		{
			mutable.Steps++;
			int totalConflicts = AnalyzeConflicts(out int weightedConflicts, variables, prepareEvaluationContext, aggregateFeasible, pairCompatible, budget, assigned, conflictCounts, conflictWeights, currentPairConflicts);
			if (mutable.BudgetExhausted)
				break;
			if (totalConflicts < mutable.BestConflictCount || totalConflicts == mutable.BestConflictCount && weightedConflicts < mutable.BestWeightedConflictCount)
			{
				mutable.BestConflictCount = totalConflicts;
				mutable.BestWeightedConflictCount = weightedConflicts;
				mutable.BestRestart = restart;
				mutable.BestStep = step;
				Array.Copy(selectedIndices, bestSelectedIndices, selectedIndices.Length);
				hasBestAssignment = true;
			}

			bool forceMove = false;
			if (totalConflicts == 0)
			{
				mutable.CompleteChecks++;
				if (acceptComplete(assigned))
				{
					solved = true;
					break;
				}

				mutable.RejectedCompleteAssignments++;
				forceMove = true;
			}

			if (totalConflicts < previousConflictCount)
				plateauSteps = 0;
			else
				plateauSteps++;
			previousConflictCount = totalConflicts;
			if (!forceMove && totalConflicts > 0 && plateauSteps >= plateauThreshold)
			{
				BumpCurrentConflictWeights(variables, conflictCounts, conflictWeights, currentPairConflicts, mutable);
				plateauSteps = 0;
			}

			int selectedVariableIndex = SelectVariable(restart, step, forceMove, variables, deterministicSeed, conflictCounts);
			mutable.LastConflictedVariable = variables[selectedVariableIndex];
			if (!RepairVariable(selectedVariableIndex, restart, step, forceMove, variables, prepareEvaluationContext, evaluate, aggregateFeasible, pairCompatible, deterministicSeed, maxCandidateEvaluations, budget, orderedDomains, assigned, variableIndices, selectedIndices, conflictCounts, conflictWeights))
				break;
		}
	}
}
