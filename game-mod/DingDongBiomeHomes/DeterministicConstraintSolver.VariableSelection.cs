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
	private static (bool flowControl, bool value) TrySelectSearchVariable<TVariable, TCandidate>(Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> currentDomains, IReadOnlyList<TVariable> variables, int maxElapsedMilliseconds, long startedMilliseconds, Dictionary<TVariable, TCandidate> assigned, Dictionary<TVariable, int> variableIndices, int[, ] constraintWeights, MutableStats<TVariable> mutable, out TVariable selected, out List<IndexedCandidate<TCandidate>> selectedRawDomain)
		where TVariable : notnull
	{
		selected = default;
		selectedRawDomain = null;
		int selectedWeightedDegree = 1;
		foreach (TVariable variable in variables)
		{
			if (!CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable))
				return (flowControl: false, value: false);
			if (assigned.ContainsKey(variable))
				continue;
			List<IndexedCandidate<TCandidate>> domain = currentDomains[variable];
			if (domain.Count == 0)
			{
				RecordDeadDomain(variable, assigned, mutable);
				return (flowControl: false, value: false);
			}

			mutable.DeadDomainsByVariable.TryGetValue(variable, out int variableFailures);
			int selectedFailures = 0;
			if (selectedRawDomain != null)
				mutable.DeadDomainsByVariable.TryGetValue(selected, out selectedFailures);
			int weightedDegree = WeightedDegree(variable, variables, assigned, variableIndices, constraintWeights);
			bool lowerDomOverWeightedDegree = selectedRawDomain != null && (long)domain.Count * selectedWeightedDegree < (long)selectedRawDomain.Count * weightedDegree;
			bool equalDomOverWeightedDegree = selectedRawDomain != null && (long)domain.Count * selectedWeightedDegree == (long)selectedRawDomain.Count * weightedDegree;
			if (selectedRawDomain == null || lowerDomOverWeightedDegree || (equalDomOverWeightedDegree && domain.Count < selectedRawDomain.Count) || (equalDomOverWeightedDegree && domain.Count == selectedRawDomain.Count && variableFailures > selectedFailures))
			{
				selected = variable;
				selectedRawDomain = domain;
				selectedWeightedDegree = weightedDegree;
			}
		}

		return (flowControl: true, value: default);
	}
}
