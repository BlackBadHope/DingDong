using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Bounded deterministic AC-3 pruning for already narrowed candidate domains.
/// A budget stop preserves only revisions backed by complete no-support proofs.
/// </summary>
public static partial class DeterministicArcConsistencyPruner
{
	public static bool TryPrune<TVariable, TCandidate>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<TVariable, TCandidate, TVariable, TCandidate, bool> pairCompatible, int maxPairEvaluations, out Dictionary<TVariable, IReadOnlyList<TCandidate>> prunedDomains, out ArcConsistencyPruneStats<TVariable> stats)
		where TVariable : notnull
	{
		return TryPruneCore(variables, domains, pairCompatible, maxPairEvaluations, initiallyRestrictedVariables: null, out prunedDomains, out stats);
	}

	/// <summary>
	/// Maintains arc consistency after one or more domains were narrowed from
	/// a previously arc-consistent snapshot. Only arcs whose support domain
	/// changed are seeded; transitive revisions are then propagated normally.
	/// </summary>
	public static bool TryPruneAfterDomainRestriction<TVariable, TCandidate>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<TVariable, TCandidate, TVariable, TCandidate, bool> pairCompatible, int maxPairEvaluations, IReadOnlyList<TVariable> restrictedVariables, out Dictionary<TVariable, IReadOnlyList<TCandidate>> prunedDomains, out ArcConsistencyPruneStats<TVariable> stats)
		where TVariable : notnull
	{
		if (restrictedVariables == null)
			throw new ArgumentNullException(nameof(restrictedVariables));
		if (restrictedVariables.Count == 0)
			throw new ArgumentException("At least one restricted variable is required.", nameof(restrictedVariables));
		return TryPruneCore(variables, domains, pairCompatible, maxPairEvaluations, restrictedVariables, out prunedDomains, out stats);
	}
}
