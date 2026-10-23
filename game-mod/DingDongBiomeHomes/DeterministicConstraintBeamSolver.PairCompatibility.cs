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
	private static bool TryPair<TVariable, TCandidate>(TVariable leftVariable, TCandidate left, TVariable rightVariable, TCandidate right, out bool compatible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, int maxPairEvaluations, MutableBeamStats<TVariable> mutable)
		where TVariable : notnull
	{
		ConstraintPairCompatibilityEvaluation pair = pairCompatible(leftVariable, left, rightVariable, right);
		mutable.PairLookups++;
		if (!pair.CacheHit && ++mutable.PairEvaluations > maxPairEvaluations)
		{
			mutable.BudgetExhausted = true;
			mutable.PairBudgetExhausted = true;
			compatible = false;
			return false;
		}

		compatible = pair.Compatible;
		return true;
	}
}
