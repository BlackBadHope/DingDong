using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Deterministic bounded min-conflicts solver. Unlike chronological
/// backtracking it always owns a complete assignment and can repair any
/// conflicted variable, which is a better fit for large, broad spatial domains.
/// The caller's complete validator remains the final acceptance oracle. When
/// no solution is found, the output still contains the best complete assignment
/// observed so an exact solver can use it as a soft value-order hint.
/// </summary>
public static partial class DeterministicConstraintRepairSolver
{
	private static int SelectVariable<TVariable>(int restart, int step, bool forceMove, IReadOnlyList<TVariable> variables, int deterministicSeed, int[] conflictCounts)
		where TVariable : notnull
	{
		int bestConflict = forceMove ? 0 : -1;
		List<int> ties = new();
		for (int i = 0; i < conflictCounts.Length; i++)
		{
			int conflict = conflictCounts[i];
			if (!forceMove && conflict <= 0)
				continue;
			if (conflict > bestConflict)
			{
				bestConflict = conflict;
				ties.Clear();
				ties.Add(i);
			}
			else if (conflict == bestConflict)
			{
				ties.Add(i);
			}
		}

		if (ties.Count == 0)
			return PositiveModulo(deterministicSeed + restart * 31 + step, variables.Count);
		return ties[PositiveModulo(deterministicSeed + restart * 97 + step * 17, ties.Count)];
	}
}
