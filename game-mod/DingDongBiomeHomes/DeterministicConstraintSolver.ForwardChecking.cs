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
	private static bool BuildForwardCheckedDomains<TVariable, TCandidate, TEvaluationContext>(Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> currentDomains, IReadOnlyList<TVariable> variables, Func<TVariable, TCandidate, TEvaluationContext, bool> forwardFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, bool enforceForwardFeasibility, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxAggregateEvaluations, int maxElapsedMilliseconds, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains, int[, ] constraintWeights, Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows, MutableStats<TVariable> mutable, TVariable selected, FeasibleCandidate<TCandidate> candidate, TEvaluationContext nextEvaluationContext, Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> nextDomains, List<TVariable> activeVariables, List<TVariable> changedSupports, bool forwardDead)
		where TVariable : notnull
	{
		foreach (TVariable variable in variables)
		{
			if (assigned.ContainsKey(variable))
				continue;
			List<IndexedCandidate<TCandidate>> filtered = new();
			if (!TryGetCompatibilityRow(selected, candidate.OriginalIndex, candidate.Value, variable, arcPhase: false, out bool[] pairRow, pairCompatible, maxArcPairEvaluations, maxForwardPairEvaluations, maxElapsedMilliseconds, startedMilliseconds, variableIndices, initialDomains, compatibilityRows, mutable))
			{
				forwardDead = true;
				break;
			}

			foreach (IndexedCandidate<TCandidate> other in currentDomains[variable])
			{
				mutable.PairChecks++;
				if (!pairRow[other.OriginalIndex])
				{
					mutable.ForwardPairValuesRemoved++;
					continue;
				}

				if (!enforceForwardFeasibility)
				{
					filtered.Add(other);
					continue;
				}

				if (mutable.Evaluations >= maxAggregateEvaluations)
				{
					mutable.AggregateBudgetExhausted = true;
					forwardDead = true;
					break;
				}

				bool aggregate = forwardFeasible(variable, other.Value, nextEvaluationContext);
				mutable.Evaluations++;
				mutable.ForwardEvaluations++;
				if (aggregate)
					filtered.Add(other);
				else
					mutable.ForwardAggregateValuesRemoved++;
			}

			if (mutable.AggregateBudgetExhausted || mutable.PairBudgetExhausted)
				break;
			nextDomains.Add(variable, filtered);
			activeVariables.Add(variable);
			if (filtered.Count < currentDomains[variable].Count)
				changedSupports.Add(variable);
			if (filtered.Count == 0)
			{
				BumpConstraintWeight(selected, variable, variableIndices, constraintWeights, mutable);
				RecordDeadDomain(variable, assigned, mutable);
				forwardDead = true;
				break;
			}
		}

		return forwardDead;
	}
}
