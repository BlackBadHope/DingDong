using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Bounded deterministic AC-3 pruning for already narrowed candidate domains.
/// A budget stop preserves only revisions backed by complete no-support proofs.
/// </summary>
public static partial class DeterministicArcConsistencyPruner
{
	private static void InitializePruningStateAndQueue<TVariable, TCandidate>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<TVariable, TCandidate, TVariable, TCandidate, bool> pairCompatible, int maxPairEvaluations, IReadOnlyList<TVariable> initiallyRestrictedVariables, out List<TCandidate>[] current, out Queue<(int Source, int Support)> queue, out bool[, ] queued, out int queuePops, out int revisions, out int pairEvaluations, out int removed, out bool budgetExhausted, out bool wipeout, out TVariable blockedVariable, out TVariable supportVariable)
		where TVariable : notnull
	{
		if (domains == null)
			throw new ArgumentNullException(nameof(domains));
		if (pairCompatible == null)
			throw new ArgumentNullException(nameof(pairCompatible));
		if (maxPairEvaluations <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxPairEvaluations));
		current = new List<TCandidate>[variables.Count];
		for (int i = 0; i < variables.Count; i++)
		{
			if (!domains.TryGetValue(variables[i], out IReadOnlyList<TCandidate> domain))
				throw new InvalidOperationException($"Missing candidate domain for variable {variables[i]}.");
			current[i] = new List<TCandidate>(domain);
		}

		queue = new();
		queued = new bool[variables.Count, variables.Count];
		if (initiallyRestrictedVariables == null)
		{
			for (int source = 0; source < variables.Count; source++)
			{
				for (int support = 0; support < variables.Count; support++)
				{
					if (source != support)
						Enqueue(source, support, queue, queued);
				}
			}
		}
		else
		{
			Dictionary<TVariable, int> variableIndices = new();
			for (int i = 0; i < variables.Count; i++)
				variableIndices.Add(variables[i], i);
			foreach (TVariable restrictedVariable in initiallyRestrictedVariables)
			{
				if (!variableIndices.TryGetValue(restrictedVariable, out int support))
					throw new InvalidOperationException($"Restricted variable {restrictedVariable} is not in the variable order.");
				for (int source = 0; source < variables.Count; source++)
				{
					if (source != support)
						Enqueue(source, support, queue, queued);
				}
			}
		}

		queuePops = 0;
		revisions = 0;
		pairEvaluations = 0;
		removed = 0;
		budgetExhausted = false;
		wipeout = false;
		blockedVariable = default;
		supportVariable = default;
	}
}
