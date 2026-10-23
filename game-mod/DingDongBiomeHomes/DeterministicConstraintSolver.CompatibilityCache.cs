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
	private static bool TryGetCompatibilityRow<TVariable, TCandidate>(TVariable source, int sourceCandidateIndex, TCandidate sourceCandidate, TVariable support, bool arcPhase, out bool[] row, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxElapsedMilliseconds, long startedMilliseconds, Dictionary<TVariable, int> variableIndices, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains, Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows, MutableStats<TVariable> mutable)
		where TVariable : notnull
	{
		if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
		{
			row = Array.Empty<bool>();
			return false;
		}

		var key = (variableIndices[source], sourceCandidateIndex, variableIndices[support]);
		if (compatibilityRows.TryGetValue(key, out bool[] cached))
		{
			row = cached;
			return true;
		}

		List<IndexedCandidate<TCandidate>> supportDomain = initialDomains[support];
		int rowLength = 0;
		foreach (IndexedCandidate<TCandidate> witness in supportDomain)
			rowLength = Math.Max(rowLength, witness.OriginalIndex + 1);
		bool[] built = new bool[rowLength];
		foreach (IndexedCandidate<TCandidate> witness in supportDomain)
		{
			if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
			{
				row = Array.Empty<bool>();
				return false;
			}

			ConstraintPairCompatibilityEvaluation pair = pairCompatible(source, sourceCandidate, support, witness.Value);
			if (arcPhase)
			{
				mutable.ArcPairLookups++;
				if (!pair.CacheHit && ++mutable.ArcPairEvaluations > maxArcPairEvaluations)
				{
					mutable.ArcBudgetExhausted = true;
					row = Array.Empty<bool>();
					return false;
				}
			}
			else
			{
				mutable.PairLookups++;
				if (!pair.CacheHit && ++mutable.PairEvaluations > maxForwardPairEvaluations)
				{
					mutable.PairBudgetExhausted = true;
					row = Array.Empty<bool>();
					return false;
				}
			}

			built[witness.OriginalIndex] = pair.Compatible;
		}

		compatibilityRows.Add(key, built);
		row = built;
		return true;
	}

	private static bool CheckTimeBudget<TVariable>(int maxElapsedMilliseconds, long startedMilliseconds, MutableStats<TVariable> mutable)
		where TVariable : notnull
	{
		if (Environment.TickCount64 - startedMilliseconds < maxElapsedMilliseconds)
			return true;
		mutable.TimeBudgetExhausted = true;
		return false;
	}
}
