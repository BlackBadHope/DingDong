using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintRepairSolver
{
	private static void BumpCurrentConflictWeights<TVariable>(IReadOnlyList<TVariable> variables, int[] conflictCounts, int[, ] conflictWeights, bool[, ] currentPairConflicts, MutableRepairStats<TVariable> mutable)
		where TVariable : notnull
	{
		int raised = 0;
		for (int i = 0; i < variables.Count; i++)
		{
			for (int j = i + 1; j < variables.Count; j++)
			{
				if (!currentPairConflicts[i, j])
					continue;
				int next = conflictWeights[i, j] + 1;
				conflictWeights[i, j] = next;
				conflictWeights[j, i] = next;
				conflictCounts[i]++;
				conflictCounts[j]++;
				raised++;
				mutable.MaxConflictWeight = Math.Max(mutable.MaxConflictWeight, next);
			}
		}

		if (raised == 0)
			return;
		mutable.WeightBreakouts++;
		mutable.WeightedEdgesRaised += raised;
	}
}
