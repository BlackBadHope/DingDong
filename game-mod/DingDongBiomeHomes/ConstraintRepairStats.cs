using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
public sealed record ConstraintRepairStats<TVariable>(int Restarts, int Steps, int Repairs, int SidewaysRepairs, int WeightBreakouts, int WeightedEdgesRaised, int MaxConflictWeight, long PairLookups, int PairEvaluations, int AggregateEvaluations, int CandidateEvaluations, int CompleteChecks, int RejectedCompleteAssignments, int BestConflictCount, int BestWeightedConflictCount, int BestRestart, int BestStep, bool BudgetExhausted, bool PairBudgetExhausted, bool CandidateBudgetExhausted, bool TimeBudgetExhausted, long ElapsedMilliseconds, TVariable LastConflictedVariable);
