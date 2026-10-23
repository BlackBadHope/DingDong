using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
public sealed record ConstraintBeamStats<TVariable>(int MaxDepth, int PeakStates, int ExpandedStates, int DeadStates, long PairLookups, int PairEvaluations, int CandidateEvaluations, int CompleteChecks, bool BudgetExhausted, bool PairBudgetExhausted, bool CandidateBudgetExhausted, TVariable LastDeadVariable, IReadOnlyList<TVariable> VariableOrder);
