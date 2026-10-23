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
	private sealed class MutableBeamStats<TVariable>
	{
		public int MaxDepth;
		public int PeakStates;
		public int ExpandedStates;
		public int DeadStates;
		public long PairLookups;
		public int PairEvaluations;
		public int CandidateEvaluations;
		public int CompleteChecks;
		public bool BudgetExhausted;
		public bool PairBudgetExhausted;
		public bool CandidateBudgetExhausted;
		public TVariable LastDeadVariable;
		public IReadOnlyList<TVariable> VariableOrder = Array.Empty<TVariable>();
		public ConstraintBeamStats<TVariable> Freeze() => new(MaxDepth, PeakStates, ExpandedStates, DeadStates, PairLookups, PairEvaluations, CandidateEvaluations, CompleteChecks, BudgetExhausted, PairBudgetExhausted, CandidateBudgetExhausted, LastDeadVariable, VariableOrder);
	}
}
