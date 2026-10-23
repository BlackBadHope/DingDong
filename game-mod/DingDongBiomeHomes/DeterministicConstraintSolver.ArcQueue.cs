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
	private static (bool flowControl, bool value) RunArcConsistencyQueue<TVariable, TCandidate>(Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> currentDomains, IReadOnlyList<TVariable> activeVariables, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxElapsedMilliseconds, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains, int[, ] constraintWeights, Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows, MutableStats<TVariable> mutable, EqualityComparer<TVariable> comparer, Queue<(TVariable Source, TVariable Support)> queue, HashSet<(TVariable Source, TVariable Support)> queued)
		where TVariable : notnull
	{
		while (queue.Count > 0)
		{
			if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
				return (flowControl: false, value: false);
			(TVariable source, TVariable support) = queue.Dequeue();
			queued.Remove((source, support));
			mutable.ArcQueuePops++;
			List<IndexedCandidate<TCandidate>> sourceDomain = currentDomains[source];
			List<IndexedCandidate<TCandidate>> supportDomain = currentDomains[support];
			List<IndexedCandidate<TCandidate>> retained = new(sourceDomain.Count);
			int removed = 0;
			foreach (IndexedCandidate<TCandidate> candidate in sourceDomain)
			{
				if (!TryGetCompatibilityRow(source, candidate.OriginalIndex, candidate.Value, support, arcPhase: true, out bool[] pairRow, pairCompatible, maxArcPairEvaluations, maxForwardPairEvaluations, maxElapsedMilliseconds, startedMilliseconds, variableIndices, initialDomains, compatibilityRows, mutable))
				{
					if (mutable.TimeBudgetExhausted)
						return (flowControl: false, value: false);
					// This arc is deliberately left unchanged: only complete
					// no-support proofs are allowed to remove a value.
					return (flowControl: false, value: true);
				}

				bool supported = false;
				foreach (IndexedCandidate<TCandidate> witness in supportDomain)
				{
					mutable.ArcPairChecks++;
					if (pairRow[witness.OriginalIndex])
					{
						supported = true;
						break;
					}
				}

				if (supported)
					retained.Add(candidate);
				else
					removed++;
			}

			if (removed == 0)
				continue;
			mutable.ArcRevisions++;
			mutable.ArcValuesRemoved += removed;
			mutable.ArcValuesRemovedByVariable.TryGetValue(source, out int previouslyRemoved);
			mutable.ArcValuesRemovedByVariable[source] = previouslyRemoved + removed;
			currentDomains[source] = retained;
			if (retained.Count == 0)
			{
				mutable.ArcWipeout = true;
				mutable.ArcWipeouts++;
				mutable.ArcBlockedVariable = source;
				mutable.ArcWitnessVariable = support;
				BumpConstraintWeight(source, support, variableIndices, constraintWeights, mutable);
				RecordDeadDomain(source, assigned, mutable);
				return (flowControl: false, value: false);
			}

			foreach (TVariable other in activeVariables)
			{
				if (!comparer.Equals(other, source) && !comparer.Equals(other, support))
					Enqueue(other, source, queue, queued);
			}
		}

		return (flowControl: true, value: default);
	}

	// Queue membership is kept alongside the FIFO so a revised arc is scheduled
	// once, while still allowing it to be enqueued again after it is processed.
	private static void Enqueue<TVariable>(TVariable source, TVariable support, Queue<(TVariable Source, TVariable Support)> queue, HashSet<(TVariable Source, TVariable Support)> queued)
		where TVariable : notnull
	{
		if (queued.Add((source, support)))
			queue.Enqueue((source, support));
	}
}
