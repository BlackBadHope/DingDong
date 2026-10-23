using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Bounded deterministic AC-3 pruning for already narrowed candidate domains.
/// A budget stop preserves only revisions backed by complete no-support proofs.
/// </summary>
public static partial class DeterministicArcConsistencyPruner
{
	private static bool TryPruneCore<TVariable, TCandidate>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<TVariable, TCandidate, TVariable, TCandidate, bool> pairCompatible, int maxPairEvaluations, IReadOnlyList<TVariable> initiallyRestrictedVariables, out Dictionary<TVariable, IReadOnlyList<TCandidate>> prunedDomains, out ArcConsistencyPruneStats<TVariable> stats)
		where TVariable : notnull
	{
		if (variables == null)
			throw new ArgumentNullException(nameof(variables));
		List<TCandidate>[] current;
		Queue<(int Source, int Support)> queue;
		bool[, ] queued;
		int queuePops, revisions, pairEvaluations, removed;
		bool budgetExhausted, wipeout;
		TVariable blockedVariable, supportVariable;
		InitializePruningStateAndQueue(variables, domains, pairCompatible, maxPairEvaluations, initiallyRestrictedVariables, out current, out queue, out queued, out queuePops, out revisions, out pairEvaluations, out removed, out budgetExhausted, out wipeout, out blockedVariable, out supportVariable);
		Dictionary<TVariable, int> removedByVariable = new();
		while (queue.Count > 0)
		{
			(int source, int support) = queue.Dequeue();
			queued[source, support] = false;
			queuePops++;
			List<TCandidate> retained = new(current[source].Count);
			int removedOnArc = 0;
			foreach (TCandidate candidate in current[source])
			{
				bool foundSupport = false;
				foreach (TCandidate witness in current[support])
				{
					if (pairEvaluations >= maxPairEvaluations)
					{
						budgetExhausted = true;
						(prunedDomains, stats) = Freeze(variables, current, queuePops, revisions, pairEvaluations, removed, budgetExhausted, wipeout, blockedVariable, supportVariable, removedByVariable);
						return true;
					}

					pairEvaluations++;
					if (pairCompatible(variables[source], candidate, variables[support], witness))
					{
						foundSupport = true;
						break;
					}
				}

				if (foundSupport)
					retained.Add(candidate);
				else
					removedOnArc++;
			}

			if (removedOnArc == 0)
				continue;
			revisions++;
			removed += removedOnArc;
			removedByVariable.TryGetValue(variables[source], out int previous);
			removedByVariable[variables[source]] = previous + removedOnArc;
			current[source] = retained;
			if (retained.Count == 0)
			{
				wipeout = true;
				blockedVariable = variables[source];
				supportVariable = variables[support];
				(prunedDomains, stats) = Freeze(variables, current, queuePops, revisions, pairEvaluations, removed, budgetExhausted, wipeout, blockedVariable, supportVariable, removedByVariable);
				return false;
			}

			for (int other = 0; other < variables.Count; other++)
			{
				if (other != source && other != support)
					Enqueue(other, source, queue, queued);
			}
		}

		(prunedDomains, stats) = Freeze(variables, current, queuePops, revisions, pairEvaluations, removed, budgetExhausted, wipeout, blockedVariable, supportVariable, removedByVariable);
		return true;
	}
}
