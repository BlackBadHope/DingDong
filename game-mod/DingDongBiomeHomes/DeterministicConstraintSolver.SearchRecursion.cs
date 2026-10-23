using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Small deterministic CSP solver used by the residence allocator. Variables
/// are selected with minimum-remaining-values (MRV) plus a deterministic
/// failure-weight tie break. Candidates are ordered least-constraining-first,
/// with current score as a tie break. Arc consistency removes values without
/// pairwise support, and forward checking rejects dead domains before the
/// search commits more layout state. Lazily materialized compatibility rows
/// avoid repeating provider lookups across backtracking branches.
/// </summary>
public static partial class DeterministicConstraintSolver
{
	private static bool Search<TVariable, TCandidate, TEvaluationContext>(Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> currentDomains, TEvaluationContext evaluationContext, IReadOnlyList<TVariable> variables, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> forwardFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, bool enforceForwardFeasibility, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxAggregateEvaluations, int maxElapsedMilliseconds, int maxNodes, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains, int[, ] constraintWeights, Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows, MutableStats<TVariable> mutable)
		where TVariable : notnull
	{
		if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
			return false;
		if (assigned.Count == variables.Count)
		{
			mutable.CompleteAssignments++;
			if (acceptComplete(assigned))
				return true;
			mutable.RejectedCompleteAssignments++;
			return false;
		}

		TVariable selected;
		List<IndexedCandidate<TCandidate>> selectedRawDomain;
		(bool selectionCanContinue, bool selectionResult) = TrySelectSearchVariable(currentDomains, variables, maxElapsedMilliseconds, startedMilliseconds, assigned, variableIndices, constraintWeights, mutable, out selected, out selectedRawDomain);
		if (!selectionCanContinue)
		{
			return selectionResult;
		}

		List<FeasibleCandidate<TCandidate>> selectedDomain;
		(bool evaluationCanContinue, bool evaluationResult) = TryEvaluateSelectedDomain(evaluationContext, evaluate, preferredCandidates, maxAggregateEvaluations, maxElapsedMilliseconds, startedMilliseconds, assigned, mutable, selected, selectedRawDomain, out selectedDomain);
		if (!evaluationCanContinue)
		{
			return evaluationResult;
		}

		(bool rankingCanContinue, bool rankingResult) = TryRankCandidatesBySupport(currentDomains, variables, pairCompatible, maxArcPairEvaluations, maxForwardPairEvaluations, maxElapsedMilliseconds, startedMilliseconds, assigned, variableIndices, initialDomains, compatibilityRows, mutable, selected, selectedDomain);
		if (!rankingCanContinue)
		{
			return rankingResult;
		}

		(bool searchCanContinue, bool searchResult) = TrySearchCandidateAssignments(currentDomains, variables, prepareEvaluationContext, evaluate, forwardFeasible, pairCompatible, acceptComplete, preferredCandidates, enforceForwardFeasibility, maxArcPairEvaluations, maxForwardPairEvaluations, maxAggregateEvaluations, maxElapsedMilliseconds, maxNodes, startedMilliseconds, assigned, variableIndices, initialDomains, constraintWeights, compatibilityRows, mutable, selected, selectedDomain);
		if (!searchCanContinue)
		{
			return searchResult;
		}

		return false;
	}
}
