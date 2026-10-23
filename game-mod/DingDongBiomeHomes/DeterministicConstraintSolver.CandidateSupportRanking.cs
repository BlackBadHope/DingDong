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
	private static (bool flowControl, bool value) TryRankCandidatesBySupport<TVariable, TCandidate>(Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> currentDomains, IReadOnlyList<TVariable> variables, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxElapsedMilliseconds, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains, Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows, MutableStats<TVariable> mutable, TVariable selected, List<FeasibleCandidate<TCandidate>> selectedDomain)
		where TVariable : notnull
	{
		for (int candidateIndex = 0; candidateIndex < selectedDomain.Count; candidateIndex++)
		{
			if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
				return (flowControl: false, value: false);
			FeasibleCandidate<TCandidate> candidate = selectedDomain[candidateIndex];
			long supportCount = 0;
			foreach (TVariable variable in variables)
			{
				if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
					return (flowControl: false, value: false);
				if (assigned.ContainsKey(variable) || EqualityComparer<TVariable>.Default.Equals(variable, selected))
					continue;
				if (!TryGetCompatibilityRow(selected, candidate.OriginalIndex, candidate.Value, variable, arcPhase: false, out bool[] pairRow, pairCompatible, maxArcPairEvaluations, maxForwardPairEvaluations, maxElapsedMilliseconds, startedMilliseconds, variableIndices, initialDomains, compatibilityRows, mutable))
					return (flowControl: false, value: false);
				foreach (IndexedCandidate<TCandidate> other in currentDomains[variable])
				{
					mutable.PairChecks++;
					if (pairRow[other.OriginalIndex])
						supportCount++;
				}
			}

			selectedDomain[candidateIndex] = candidate with
			{
				SupportCount = supportCount
			};
		}

		mutable.ValueOrderings++;
		selectedDomain.Sort(static (left, right) =>
		{
			int support = right.SupportCount.CompareTo(left.SupportCount);
			if (support != 0)
				return support;
			int preferred = right.Preferred.CompareTo(left.Preferred);
			if (preferred != 0)
				return preferred;
			int score = right.Score.CompareTo(left.Score);
			return score != 0 ? score : left.OriginalIndex.CompareTo(right.OriginalIndex);
		});
		return (flowControl: true, value: default);
	}
}
