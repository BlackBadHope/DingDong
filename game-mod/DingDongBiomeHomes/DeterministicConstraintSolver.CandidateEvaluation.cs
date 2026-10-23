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
	private static (bool flowControl, bool value) TryEvaluateSelectedDomain<TVariable, TCandidate, TEvaluationContext>(TEvaluationContext evaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, int maxAggregateEvaluations, int maxElapsedMilliseconds, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, MutableStats<TVariable> mutable, TVariable selected, List<IndexedCandidate<TCandidate>> selectedRawDomain, out List<FeasibleCandidate<TCandidate>> selectedDomain)
		where TVariable : notnull
	{
		selectedDomain = new(selectedRawDomain.Count);
		foreach (IndexedCandidate<TCandidate> candidate in selectedRawDomain)
		{
			if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
				return (flowControl: false, value: false);
			if (mutable.Evaluations >= maxAggregateEvaluations)
			{
				mutable.AggregateBudgetExhausted = true;
				return (flowControl: false, value: false);
			}

			ConstraintCandidateEvaluation result = evaluate(selected, candidate.Value, evaluationContext);
			mutable.Evaluations++;
			mutable.SelectionEvaluations++;
			if (result.Feasible)
			{
				bool preferred = preferredCandidates != null && preferredCandidates.TryGetValue(selected, out TCandidate preferredCandidate) && EqualityComparer<TCandidate>.Default.Equals(candidate.Value, preferredCandidate);
				selectedDomain.Add(new FeasibleCandidate<TCandidate>(candidate.Value, result.Score, candidate.OriginalIndex, 0, preferred));
			}
		}

		if (selectedDomain.Count == 0)
		{
			RecordDeadDomain(selected, assigned, mutable);
			return (flowControl: false, value: false);
		}

		return (flowControl: true, value: default);
	}
}
