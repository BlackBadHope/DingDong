using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintBeamSolver
{
	private static void ValidateBeamSearchInputs<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, int beamWidth, int candidateFanout, int statesPerDiversityBucket, int maxPairEvaluations, int maxCandidateEvaluations)
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
		if (pairCompatible == null)
			throw new ArgumentNullException(nameof(pairCompatible));
		if (acceptComplete == null)
			throw new ArgumentNullException(nameof(acceptComplete));
		if (beamWidth <= 0 || candidateFanout <= 0 || statesPerDiversityBucket <= 0)
			throw new ArgumentOutOfRangeException(nameof(beamWidth));
		if (maxPairEvaluations <= 0 || maxCandidateEvaluations <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxPairEvaluations));
	}
}
