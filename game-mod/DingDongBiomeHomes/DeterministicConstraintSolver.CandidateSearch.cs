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
	private static (bool flowControl, bool value) TrySearchCandidateAssignments<TVariable, TCandidate, TEvaluationContext>(Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> currentDomains, IReadOnlyList<TVariable> variables, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> forwardFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, bool enforceForwardFeasibility, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxAggregateEvaluations, int maxElapsedMilliseconds, int maxNodes, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains, int[, ] constraintWeights, Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows, MutableStats<TVariable> mutable, TVariable selected, List<FeasibleCandidate<TCandidate>> selectedDomain)
		where TVariable : notnull
	{
		foreach (FeasibleCandidate<TCandidate> candidate in selectedDomain)
		{
			if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
				return (flowControl: false, value: false);
			if (mutable.NodesVisited >= maxNodes)
			{
				mutable.BudgetExhausted = true;
				return (flowControl: false, value: false);
			}

			mutable.NodesVisited++;
			assigned.Add(selected, candidate.Value);
			if (assigned.Count > mutable.MaxAssigned)
				mutable.MaxAssigned = assigned.Count;
			TEvaluationContext nextEvaluationContext = prepareEvaluationContext(assigned);
			Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> nextDomains = new();
			List<TVariable> activeVariables = new();
			List<TVariable> changedSupports = new();
			bool forwardDead = false;
			forwardDead = BuildForwardCheckedDomains(currentDomains, variables, forwardFeasible, pairCompatible, enforceForwardFeasibility, maxArcPairEvaluations, maxForwardPairEvaluations, maxAggregateEvaluations, maxElapsedMilliseconds, startedMilliseconds, assigned, variableIndices, initialDomains, constraintWeights, compatibilityRows, mutable, selected, candidate, nextEvaluationContext, nextDomains, activeVariables, changedSupports, forwardDead);
			if (!forwardDead && changedSupports.Count > 0 && !mutable.ArcBudgetExhausted && !EnforceArcConsistency(nextDomains, activeVariables, changedSupports, seedAll: false, pairCompatible, maxArcPairEvaluations, maxForwardPairEvaluations, maxElapsedMilliseconds, startedMilliseconds, assigned, variableIndices, initialDomains, constraintWeights, compatibilityRows, mutable))
			{
				forwardDead = true;
			}

			if (!forwardDead && Search(nextDomains, nextEvaluationContext, variables, prepareEvaluationContext, evaluate, forwardFeasible, pairCompatible, acceptComplete, preferredCandidates, enforceForwardFeasibility, maxArcPairEvaluations, maxForwardPairEvaluations, maxAggregateEvaluations, maxElapsedMilliseconds, maxNodes, startedMilliseconds, assigned, variableIndices, initialDomains, constraintWeights, compatibilityRows, mutable))
				return (flowControl: false, value: true);
			assigned.Remove(selected);
			mutable.Backtracks++;
			if (mutable.BudgetExhausted || mutable.AggregateBudgetExhausted || mutable.PairBudgetExhausted || mutable.TimeBudgetExhausted)
				return (flowControl: false, value: false);
		}

		return (flowControl: true, value: default);
	}
}
