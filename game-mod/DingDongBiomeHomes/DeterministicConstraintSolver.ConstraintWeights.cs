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
	private static void RecordDeadDomain<TVariable, TCandidate>(TVariable variable, Dictionary<TVariable, TCandidate> assigned, MutableStats<TVariable> mutable)
		where TVariable : notnull
	{
		mutable.DeadDomains++;
		mutable.DeadDomainsByVariable.TryGetValue(variable, out int previous);
		mutable.DeadDomainsByVariable[variable] = previous + 1;
		if (assigned.Count < mutable.DeepestBlockedDepth)
			return;
		mutable.DeepestBlockedDepth = assigned.Count;
		mutable.BlockedVariable = variable;
		mutable.DeepestPath = new List<TVariable>(assigned.Keys);
	}

	private static void BumpConstraintWeight<TVariable>(TVariable left, TVariable right, Dictionary<TVariable, int> variableIndices, int[, ] constraintWeights, MutableStats<TVariable> mutable)
		where TVariable : notnull
	{
		int leftIndex = variableIndices[left];
		int rightIndex = variableIndices[right];
		int next = constraintWeights[leftIndex, rightIndex] + 1;
		constraintWeights[leftIndex, rightIndex] = next;
		constraintWeights[rightIndex, leftIndex] = next;
		mutable.ConstraintWeightUpdates++;
		mutable.MaxConstraintWeight = Math.Max(mutable.MaxConstraintWeight, next);
	}

	private static int WeightedDegree<TVariable, TCandidate>(TVariable variable, IReadOnlyList<TVariable> variables, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, int[, ] constraintWeights)
		where TVariable : notnull
	{
		int index = variableIndices[variable];
		int total = 0;
		foreach (TVariable other in variables)
		{
			if (assigned.ContainsKey(other) || EqualityComparer<TVariable>.Default.Equals(variable, other))
				continue;
			total += constraintWeights[index, variableIndices[other]];
		}

		return Math.Max(1, total);
	}
}
