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
	public static bool TrySolve<TVariable, TCandidate, TEvaluationContext>(IReadOnlyList<TVariable> variables, IReadOnlyDictionary<TVariable, IReadOnlyList<TCandidate>> domains, Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, Func<IReadOnlyDictionary<TVariable, TCandidate>, bool> acceptComplete, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, int beamWidth, int candidateFanout, int statesPerDiversityBucket, int maxPairEvaluations, int maxCandidateEvaluations, out List<KeyValuePair<TVariable, TCandidate>> solution, out ConstraintBeamStats<TVariable> stats)
		where TVariable : notnull
	{
		ValidateBeamSearchInputs(variables, domains, prepareEvaluationContext, evaluate, pairCompatible, acceptComplete, beamWidth, candidateFanout, statesPerDiversityBucket, maxPairEvaluations, maxCandidateEvaluations);
		Dictionary<TVariable, int> originalIndices = new();
		for (int i = 0; i < variables.Count; i++)
		{
			TVariable variable = variables[i];
			originalIndices.Add(variable, i);
			if (!domains.TryGetValue(variable, out IReadOnlyList<TCandidate> domain))
				throw new InvalidOperationException($"Missing candidate domain for variable {variable}.");
			if (domain.Count == 0)
				throw new InvalidOperationException($"Empty candidate domain for variable {variable}.");
		}

		MutableBeamStats<TVariable> mutable;
		Dictionary<TVariable, int> preferredConflictCounts;
		AnalyzePreferredCandidateConflicts(variables, pairCompatible, preferredCandidates, maxPairEvaluations, out mutable, out preferredConflictCounts);
		List<TVariable> orderedVariables = new(variables);
		orderedVariables.Sort((left, right) =>
		{
			int conflicts = preferredConflictCounts[right].CompareTo(preferredConflictCounts[left]);
			if (conflicts != 0)
				return conflicts;
			int domain = domains[left].Count.CompareTo(domains[right].Count);
			return domain != 0 ? domain : originalIndices[left].CompareTo(originalIndices[right]);
		});
		mutable.VariableOrder = new List<TVariable>(orderedVariables);
		BeamFrontier<TVariable, TCandidate> frontier = new();
		RunBeamExpansion(domains, prepareEvaluationContext, evaluate, pairCompatible, preferredCandidates, beamWidth, candidateFanout, statesPerDiversityBucket, maxPairEvaluations, maxCandidateEvaluations, mutable, orderedVariables, frontier);
		solution = new List<KeyValuePair<TVariable, TCandidate>>();
		if (!mutable.BudgetExhausted && mutable.MaxDepth == variables.Count)
		{
			foreach (BeamFrontier<TVariable, TCandidate>.State state in frontier.States)
			{
				mutable.CompleteChecks++;
				if (!acceptComplete(state.Assigned))
					continue;
				foreach (TVariable variable in variables)
					solution.Add(new KeyValuePair<TVariable, TCandidate>(variable, state.Assigned[variable]));
				stats = mutable.Freeze();
				return true;
			}
		}

		stats = mutable.Freeze();
		return false;
	}
}
