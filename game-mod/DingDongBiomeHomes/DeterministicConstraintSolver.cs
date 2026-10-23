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
	public static bool TrySolve<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TEvaluationContext, bool> forwardFeasible, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, bool enforceInitialArcConsistency, bool enforceForwardFeasibility, int maxArcPairEvaluations, int maxForwardPairEvaluations, int maxAggregateEvaluations, int maxElapsedMilliseconds, int maxNodes, out List<KeyValuePair<TVariable, TCandidate>> solution, out ConstraintSolverStats<TVariable> stats)
		where TVariable : notnull => TrySolve(variables, domains, prepareEvaluationContext, evaluate, forwardFeasible, pairCompatible, acceptComplete, null, enforceInitialArcConsistency, enforceForwardFeasibility, maxArcPairEvaluations, maxForwardPairEvaluations, maxAggregateEvaluations, maxElapsedMilliseconds, maxNodes, out solution, out stats);
}
