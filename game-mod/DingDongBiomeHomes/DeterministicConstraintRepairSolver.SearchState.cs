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
	private readonly record struct CandidateChoice(int Index, int Score);
	private sealed class MutableRepairStats<TVariable>
	{
		public int Restarts;
		public int Steps;
		public int Repairs;
		public int SidewaysRepairs;
		public int WeightBreakouts;
		public int WeightedEdgesRaised;
		public int MaxConflictWeight;
		public long PairLookups;
		public int PairEvaluations;
		public int AggregateEvaluations;
		public int CandidateEvaluations;
		public int CompleteChecks;
		public int RejectedCompleteAssignments;
		public int BestConflictCount;
		public int BestWeightedConflictCount;
		public int BestRestart;
		public int BestStep;
		public bool BudgetExhausted;
		public bool PairBudgetExhausted;
		public bool CandidateBudgetExhausted;
		public bool TimeBudgetExhausted;
		public long ElapsedMilliseconds;
		public TVariable LastConflictedVariable;
		public ConstraintRepairStats<TVariable> Freeze() => new(Restarts, Steps, Repairs, SidewaysRepairs, WeightBreakouts, WeightedEdgesRaised, MaxConflictWeight, PairLookups, PairEvaluations, AggregateEvaluations, CandidateEvaluations, CompleteChecks, RejectedCompleteAssignments, BestConflictCount, BestWeightedConflictCount, BestRestart, BestStep, BudgetExhausted, PairBudgetExhausted, CandidateBudgetExhausted, TimeBudgetExhausted, ElapsedMilliseconds, LastConflictedVariable);
	}
}
