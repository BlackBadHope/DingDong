using System;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintRepairSolver
{
	/// <summary>
	/// Owns the repair search's shared deadline, uncached-pair allowance, and
	/// mutable evidence counters so every evaluation path observes one budget.
	/// </summary>
	private sealed class RepairEvaluationBudget<TVariable>
		where TVariable : notnull
	{
		private readonly int maxPairEvaluations;
		private readonly int maxElapsedMilliseconds;

		public long StartedMilliseconds { get; } = Environment.TickCount64;
		public MutableRepairStats<TVariable> Stats { get; } = new()
		{
			BestConflictCount = int.MaxValue,
			BestWeightedConflictCount = int.MaxValue,
			MaxConflictWeight = 1
		};

		public RepairEvaluationBudget(int maxPairEvaluations, int maxElapsedMilliseconds)
		{
			this.maxPairEvaluations = maxPairEvaluations;
			this.maxElapsedMilliseconds = maxElapsedMilliseconds;
		}

		public bool IsExpired()
		{
			if (Stats.TimeBudgetExhausted)
				return true;
			if (Environment.TickCount64 - StartedMilliseconds < maxElapsedMilliseconds)
				return false;
			Stats.BudgetExhausted = true;
			Stats.TimeBudgetExhausted = true;
			return true;
		}

		public bool TryPair<TCandidate>(TVariable leftVariable, TCandidate left, TVariable rightVariable, TCandidate right, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, out bool compatible)
		{
			if (IsExpired())
			{
				compatible = false;
				return false;
			}

			ConstraintPairCompatibilityEvaluation pair = pairCompatible(leftVariable, left, rightVariable, right);
			Stats.PairLookups++;
			if (IsExpired())
			{
				compatible = false;
				return false;
			}

			if (!pair.CacheHit && ++Stats.PairEvaluations > maxPairEvaluations)
			{
				Stats.BudgetExhausted = true;
				Stats.PairBudgetExhausted = true;
				compatible = false;
				return false;
			}

			compatible = pair.Compatible;
			return true;
		}

		public void Complete() => Stats.ElapsedMilliseconds = Environment.TickCount64 - StartedMilliseconds;
	}
}
