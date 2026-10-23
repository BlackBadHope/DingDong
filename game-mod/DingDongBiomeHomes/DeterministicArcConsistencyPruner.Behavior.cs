using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Bounded deterministic AC-3 pruning for already narrowed candidate domains.
/// A budget stop preserves only revisions backed by complete no-support proofs.
/// </summary>
public static partial class DeterministicArcConsistencyPruner
{
	private static (Dictionary<TVariable, IReadOnlyList<TCandidate>> Domains, ArcConsistencyPruneStats<TVariable> Stats) Freeze<TVariable, TCandidate>(IReadOnlyList<TVariable> variables, List<TCandidate>[] current, int queuePops, int revisions, int pairEvaluations, int removed, bool budgetExhausted, bool wipeout, TVariable blockedVariable, TVariable supportVariable, Dictionary<TVariable, int> removedByVariable)
		where TVariable : notnull
	{
		Dictionary<TVariable, IReadOnlyList<TCandidate>> domainsSnapshot = new();
		for (int i = 0; i < variables.Count; i++)
			domainsSnapshot.Add(variables[i], current[i]);
		ArcConsistencyPruneStats<TVariable> statsSnapshot = new(queuePops, revisions, pairEvaluations, removed, budgetExhausted, wipeout, blockedVariable, supportVariable, new Dictionary<TVariable, int>(removedByVariable));
		return (domainsSnapshot, statsSnapshot);
	}
}
