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
	private static void ResetConflictWeights<TVariable>(IReadOnlyList<TVariable> variables, int[, ] conflictWeights)
		where TVariable : notnull
	{
		for (int i = 0; i < variables.Count; i++)
		{
			for (int j = 0; j < variables.Count; j++)
				conflictWeights[i, j] = i == j ? 0 : 1;
		}
	}

	private static void Initialize<TVariable, TCandidate>(int restart, IReadOnlyList<TVariable> variables, int deterministicSeed, List<IReadOnlyList<TCandidate>> orderedDomains, Dictionary<TVariable, TCandidate> assigned, int[] selectedIndices)
		where TVariable : notnull
	{
		assigned.Clear();
		for (int i = 0; i < variables.Count; i++)
		{
			int count = orderedDomains[i].Count;
			int index = restart == 0 ? 0 : PositiveModulo(deterministicSeed + restart * 104729 + i * 8191 + i * i * 131, count);
			selectedIndices[i] = index;
			assigned.Add(variables[i], orderedDomains[i][index]);
		}
	}
}
