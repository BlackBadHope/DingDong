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
	private static void ValidateRepairSearchInputs<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> aggregateFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, int maxRestarts, int maxStepsPerRestart, int maxPairEvaluations, int maxCandidateEvaluations, int maxElapsedMilliseconds)
		where TVariable : notnull
	{
		if (variables == null)
			throw new ArgumentNullException(nameof(variables));
		if (domains == null)
			throw new ArgumentNullException(nameof(domains));
		if (prepareEvaluationContext == null)
			throw new ArgumentNullException(nameof(prepareEvaluationContext));
		if (evaluate == null)
			throw new ArgumentNullException(nameof(evaluate));
		if (aggregateFeasible == null)
			throw new ArgumentNullException(nameof(aggregateFeasible));
		if (pairCompatible == null)
			throw new ArgumentNullException(nameof(pairCompatible));
		if (acceptComplete == null)
			throw new ArgumentNullException(nameof(acceptComplete));
		if (maxRestarts <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxRestarts));
		if (maxStepsPerRestart <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxStepsPerRestart));
		if (maxPairEvaluations <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxPairEvaluations));
		if (maxCandidateEvaluations <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxCandidateEvaluations));
		if (maxElapsedMilliseconds <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxElapsedMilliseconds));
	}
}
