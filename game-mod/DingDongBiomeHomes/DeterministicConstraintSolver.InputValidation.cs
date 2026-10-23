using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintSolver
{
	private static void ValidateConstraintSearchInputs<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> forwardFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxAggregateEvaluations, int maxElapsedMilliseconds, int maxNodes)
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
		if (forwardFeasible == null)
			throw new ArgumentNullException(nameof(forwardFeasible));
		if (pairCompatible == null)
			throw new ArgumentNullException(nameof(pairCompatible));
		if (acceptComplete == null)
			throw new ArgumentNullException(nameof(acceptComplete));
		if (maxArcPairEvaluations <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxArcPairEvaluations));
		if (maxForwardPairEvaluations <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxForwardPairEvaluations));
		if (maxAggregateEvaluations <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxAggregateEvaluations));
		if (maxElapsedMilliseconds <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxElapsedMilliseconds));
		if (maxNodes <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxNodes));
	}
}
