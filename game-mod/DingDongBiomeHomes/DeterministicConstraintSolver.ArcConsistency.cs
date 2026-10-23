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
	private static bool EnforceArcConsistency<TVariable, TCandidate>(Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> currentDomains, IReadOnlyList<TVariable> activeVariables, IReadOnlyCollection<TVariable> changedSupports, bool seedAll, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxElapsedMilliseconds, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains, int[, ] constraintWeights, Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows, MutableStats<TVariable> mutable)
		where TVariable : notnull
	{
		if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
			return false;
		EqualityComparer<TVariable> comparer = EqualityComparer<TVariable>.Default;
		Queue<(TVariable Source, TVariable Support)> queue = new();
		HashSet<(TVariable Source, TVariable Support)> queued = new();
		if (seedAll)
		{
			foreach (TVariable source in activeVariables)
			{
				foreach (TVariable support in activeVariables)
				{
					if (!comparer.Equals(source, support))
						Enqueue(source, support, queue, queued);
				}
			}
		}
		else
		{
			foreach (TVariable support in changedSupports)
			{
				foreach (TVariable source in activeVariables)
				{
					if (!comparer.Equals(source, support))
						Enqueue(source, support, queue, queued);
				}
			}
		}

		(bool flowControl, bool value) = RunArcConsistencyQueue(currentDomains, activeVariables, pairCompatible, maxArcPairEvaluations, maxForwardPairEvaluations, maxElapsedMilliseconds, startedMilliseconds, assigned, variableIndices, initialDomains, constraintWeights, compatibilityRows, mutable, comparer, queue, queued);
		if (!flowControl)
		{
			return value;
		}

		return true;
	}
}
