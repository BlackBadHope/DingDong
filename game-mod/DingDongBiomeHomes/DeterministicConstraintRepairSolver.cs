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
	public static bool TrySolve<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> aggregateFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, int deterministicSeed, int maxRestarts, int maxStepsPerRestart, int maxPairEvaluations, int maxCandidateEvaluations, int maxElapsedMilliseconds, out List<KeyValuePair<TVariable, TCandidate>> solution, out ConstraintRepairStats<TVariable> stats)
		where TVariable : notnull
	{
		ValidateRepairSearchInputs(variables, domains, prepareEvaluationContext, evaluate, aggregateFeasible, pairCompatible, acceptComplete, maxRestarts, maxStepsPerRestart, maxPairEvaluations, maxCandidateEvaluations, maxElapsedMilliseconds);
		RepairEvaluationBudget<TVariable> budget = new(maxPairEvaluations, maxElapsedMilliseconds);
		List<IReadOnlyList<TCandidate>> orderedDomains = new(variables.Count);
		foreach (TVariable variable in variables)
		{
			if (!domains.TryGetValue(variable, out IReadOnlyList<TCandidate> domain))
				throw new InvalidOperationException($"Missing candidate domain for variable {variable}.");
			if (domain.Count == 0)
				throw new InvalidOperationException($"Empty candidate domain for variable {variable}.");
			orderedDomains.Add(domain);
		}

		Dictionary<TVariable, TCandidate> assigned = new();
		Dictionary<TVariable, int> variableIndices = new();
		for (int i = 0; i < variables.Count; i++)
			variableIndices.Add(variables[i], i);
		int[] selectedIndices = new int[variables.Count];
		int[] bestSelectedIndices = new int[variables.Count];
		bool hasBestAssignment = false;
		int[] conflictCounts = new int[variables.Count];
		int[, ] conflictWeights = new int[variables.Count, variables.Count];
		bool[, ] currentPairConflicts = new bool[variables.Count, variables.Count];
		MutableRepairStats<TVariable> mutable = budget.Stats;
		bool solved = false;
		int plateauThreshold = Math.Max(4, variables.Count / 4);
		RunRepairRestarts(variables, prepareEvaluationContext, evaluate, aggregateFeasible, pairCompatible, acceptComplete, deterministicSeed, maxRestarts, maxStepsPerRestart, maxCandidateEvaluations, budget, orderedDomains, assigned, variableIndices, selectedIndices, bestSelectedIndices, ref hasBestAssignment, conflictCounts, conflictWeights, currentPairConflicts, ref solved, plateauThreshold);
		solution = new List<KeyValuePair<TVariable, TCandidate>>(variables.Count);
		if (solved)
		{
			foreach (TVariable variable in variables)
				solution.Add(new KeyValuePair<TVariable, TCandidate>(variable, assigned[variable]));
		}
		else if (hasBestAssignment)
		{
			for (int i = 0; i < variables.Count; i++)
				solution.Add(new KeyValuePair<TVariable, TCandidate>(variables[i], orderedDomains[i][bestSelectedIndices[i]]));
		}

		budget.Complete();
		stats = mutable.Freeze();
		return solved;
	}
}
