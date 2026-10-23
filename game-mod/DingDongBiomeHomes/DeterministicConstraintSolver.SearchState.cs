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
	private readonly record struct IndexedCandidate<TCandidate>(TCandidate Value, int OriginalIndex);
	private readonly record struct FeasibleCandidate<TCandidate>(TCandidate Value, int Score, int OriginalIndex, long SupportCount, bool Preferred);
	private sealed class MutableStats<TVariable>
	{
		public int NodesVisited;
		public int Evaluations;
		public int SelectionEvaluations;
		public int ForwardEvaluations;
		public long PairChecks;
		public long PairLookups;
		public int PairEvaluations;
		public int ForwardPairValuesRemoved;
		public int ForwardAggregateValuesRemoved;
		public int ArcQueuePops;
		public int ArcRevisions;
		public long ArcPairChecks;
		public long ArcPairLookups;
		public int ArcPairEvaluations;
		public int ArcValuesRemoved;
		public int ArcWipeouts;
		public int Backtracks;
		public int DeadDomains;
		public int CompleteAssignments;
		public int RejectedCompleteAssignments;
		public int MaxAssigned;
		public int ValueOrderings;
		public int ConstraintWeightUpdates;
		public int MaxConstraintWeight = 1;
		public long ElapsedMilliseconds;
		public bool BudgetExhausted;
		public bool TimeBudgetExhausted;
		public bool AggregateBudgetExhausted;
		public bool PairBudgetExhausted;
		public bool ArcBudgetExhausted;
		public bool ArcWipeout;
		public TVariable BlockedVariable;
		public TVariable ArcBlockedVariable;
		public TVariable ArcWitnessVariable;
		public int DeepestBlockedDepth = -1;
		public IReadOnlyList<TVariable> DeepestPath = Array.Empty<TVariable>();
		public Dictionary<TVariable, int> ArcValuesRemovedByVariable = new();
		public Dictionary<TVariable, int> DeadDomainsByVariable = new();
		public ConstraintSolverStats<TVariable> Freeze() => new(NodesVisited, Evaluations, SelectionEvaluations, ForwardEvaluations, PairChecks, PairLookups, PairEvaluations, ForwardPairValuesRemoved, ForwardAggregateValuesRemoved, ArcQueuePops, ArcRevisions, ArcPairChecks, ArcPairLookups, ArcPairEvaluations, ArcValuesRemoved, ArcWipeouts, Backtracks, DeadDomains, CompleteAssignments, RejectedCompleteAssignments, MaxAssigned, ValueOrderings, ConstraintWeightUpdates, MaxConstraintWeight, ElapsedMilliseconds, BudgetExhausted, TimeBudgetExhausted, AggregateBudgetExhausted, PairBudgetExhausted, ArcBudgetExhausted, ArcWipeout, BlockedVariable, ArcBlockedVariable, ArcWitnessVariable, DeepestPath, new Dictionary<TVariable, int>(ArcValuesRemovedByVariable), new Dictionary<TVariable, int>(DeadDomainsByVariable));
	}
}
