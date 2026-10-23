using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Deterministic bounded constructive CSP search. It keeps a diverse beam of
/// complete partial assignments and is intended to find one good layout after
/// min-conflicts has identified the high-pressure variables. The caller's hard
/// pair, aggregate and complete validators remain authoritative.
/// </summary>
public static partial class DeterministicConstraintBeamSolver
{
	private static void AnalyzePreferredCandidateConflicts<TVariable, TCandidate>(IReadOnlyList<TVariable> variables, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, int maxPairEvaluations, out MutableBeamStats<TVariable> mutable, out Dictionary<TVariable, int> preferredConflictCounts)
		where TVariable : notnull
	{
		mutable = new();
		preferredConflictCounts = new();
		foreach (TVariable variable in variables)
			preferredConflictCounts.Add(variable, 0);
		if (preferredCandidates != null && preferredCandidates.Count == variables.Count)
		{
			for (int leftIndex = 0; leftIndex < variables.Count; leftIndex++)
			{
				TVariable left = variables[leftIndex];
				for (int rightIndex = leftIndex + 1; rightIndex < variables.Count; rightIndex++)
				{
					TVariable right = variables[rightIndex];
					if (!TryPair(left, preferredCandidates[left], right, preferredCandidates[right], out bool compatible, pairCompatible, maxPairEvaluations, mutable))
						break;
					if (compatible)
						continue;
					preferredConflictCounts[left]++;
					preferredConflictCounts[right]++;
				}

				if (mutable.BudgetExhausted)
					break;
			}
		}
	}

}
