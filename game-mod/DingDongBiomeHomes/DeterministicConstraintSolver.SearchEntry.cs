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
	public static bool TrySolve<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> forwardFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, bool enforceInitialArcConsistency, bool enforceForwardFeasibility, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxAggregateEvaluations, int maxElapsedMilliseconds, int maxNodes, out List<KeyValuePair<TVariable, TCandidate>> solution, out ConstraintSolverStats<TVariable> stats)
		where TVariable : notnull
	{
		ValidateConstraintSearchInputs(variables, domains, prepareEvaluationContext, evaluate, forwardFeasible, pairCompatible, acceptComplete, maxArcPairEvaluations, maxForwardPairEvaluations, maxAggregateEvaluations, maxElapsedMilliseconds, maxNodes);
		long startedMilliseconds = Environment.TickCount64;
		Dictionary<TVariable, TCandidate> assigned = new();
		Dictionary<TVariable, int> variableIndices = new();
		Dictionary<TVariable, List<IndexedCandidate<TCandidate>>> initialDomains = new();
		for (int variableIndex = 0; variableIndex < variables.Count; variableIndex++)
		{
			TVariable variable = variables[variableIndex];
			if (!domains.TryGetValue(variable, out IReadOnlyList<TCandidate> domain))
				throw new InvalidOperationException($"Missing candidate domain for variable {variable}.");
			List<IndexedCandidate<TCandidate>> indexed = new(domain.Count);
			for (int i = 0; i < domain.Count; i++)
				indexed.Add(new IndexedCandidate<TCandidate>(domain[i], i));
			variableIndices.Add(variable, variableIndex);
			initialDomains.Add(variable, indexed);
		}

		int[, ] constraintWeights = new int[variables.Count, variables.Count];
		for (int left = 0; left < variables.Count; left++)
		{
			for (int right = 0; right < variables.Count; right++)
				constraintWeights[left, right] = left == right ? 0 : 1;
		}

		Dictionary<(int SourceVariable, int SourceCandidate, int SupportVariable), bool[]> compatibilityRows = new();
		MutableStats<TVariable> mutable = new();
		bool arcConsistent = CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable) && (!enforceInitialArcConsistency || EnforceArcConsistency(initialDomains, variables, Array.Empty<TVariable>(), seedAll: true, pairCompatible, maxArcPairEvaluations, maxForwardPairEvaluations, maxElapsedMilliseconds, startedMilliseconds, assigned, variableIndices, initialDomains, constraintWeights, compatibilityRows, mutable));
		TEvaluationContext initialContext = prepareEvaluationContext(assigned);
		bool solved = arcConsistent && CheckTimeBudget(maxElapsedMilliseconds, startedMilliseconds, mutable) && Search(initialDomains, initialContext, variables, prepareEvaluationContext, evaluate, forwardFeasible, pairCompatible, acceptComplete, preferredCandidates, enforceForwardFeasibility, maxArcPairEvaluations, maxForwardPairEvaluations, maxAggregateEvaluations, maxElapsedMilliseconds, maxNodes, startedMilliseconds, assigned, variableIndices, initialDomains, constraintWeights, compatibilityRows, mutable);
		solution = new List<KeyValuePair<TVariable, TCandidate>>();
		if (solved)
		{
			foreach (TVariable variable in variables)
				solution.Add(new KeyValuePair<TVariable, TCandidate>(variable, assigned[variable]));
		}

		mutable.ElapsedMilliseconds = Environment.TickCount64 - startedMilliseconds;
		stats = mutable.Freeze();
		return solved;
	}
}
