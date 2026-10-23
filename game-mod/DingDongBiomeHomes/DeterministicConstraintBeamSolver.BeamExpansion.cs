using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintBeamSolver
{
	private static void RunBeamExpansion<TVariable, TCandidate, TEvaluationContext>(IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, int beamWidth, int candidateFanout, int statesPerDiversityBucket, int maxPairEvaluations, int maxCandidateEvaluations, MutableBeamStats<TVariable> mutable, List<TVariable> orderedVariables, BeamFrontier<TVariable, TCandidate> frontier)
		where TVariable : notnull
	{
		for (int depth = 0; depth < orderedVariables.Count && !mutable.BudgetExhausted; depth++)
		{
			TVariable variable = orderedVariables[depth];
			IReadOnlyList<TCandidate> domain = domains[variable];
			List<BeamFrontier<TVariable, TCandidate>.State> expanded = new();
			ExpandBeamStates(prepareEvaluationContext, evaluate, pairCompatible, preferredCandidates, candidateFanout, maxPairEvaluations, maxCandidateEvaluations, mutable, frontier.States, variable, domain, expanded);
			if (mutable.BudgetExhausted || expanded.Count == 0)
				break;
			frontier.Advance(expanded, beamWidth, statesPerDiversityBucket);
			mutable.PeakStates = Math.Max(mutable.PeakStates, frontier.Count);
			mutable.MaxDepth = depth + 1;
		}

	}
}
