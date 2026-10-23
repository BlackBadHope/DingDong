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
	private static void ExpandBeamStates<TVariable, TCandidate, TEvaluationContext>(Func<IReadOnlyDictionary<TVariable, TCandidate>, TEvaluationContext> prepareEvaluationContext, Func<TVariable, TCandidate, TEvaluationContext, ConstraintCandidateEvaluation> evaluate, Func<TVariable, TCandidate, TVariable, TCandidate, ConstraintPairCompatibilityEvaluation> pairCompatible, IReadOnlyDictionary<TVariable, TCandidate> preferredCandidates, int candidateFanout, int maxPairEvaluations, int maxCandidateEvaluations, MutableBeamStats<TVariable> mutable, IReadOnlyList<BeamFrontier<TVariable, TCandidate>.State> states, TVariable variable, IReadOnlyList<TCandidate> domain, List<BeamFrontier<TVariable, TCandidate>.State> expanded)
		where TVariable : notnull
	{
		foreach (BeamFrontier<TVariable, TCandidate>.State state in states)
		{
			TEvaluationContext context = prepareEvaluationContext(state.Assigned);
			List<BeamFrontier<TVariable, TCandidate>.Choice> choices = new();
			for (int candidateIndex = 0; candidateIndex < domain.Count; candidateIndex++)
			{
				TCandidate candidate = domain[candidateIndex];
				bool compatible = true;
				foreach (KeyValuePair<TVariable, TCandidate> other in state.Assigned)
				{
					if (!TryPair(variable, candidate, other.Key, other.Value, out bool pairAccepted, pairCompatible, maxPairEvaluations, mutable))
					{
						compatible = false;
						break;
					}

					if (!pairAccepted)
					{
						compatible = false;
						break;
					}
				}

				if (mutable.BudgetExhausted)
					break;
				if (!compatible)
					continue;
				if (mutable.CandidateEvaluations >= maxCandidateEvaluations)
				{
					mutable.BudgetExhausted = true;
					mutable.CandidateBudgetExhausted = true;
					break;
				}

				ConstraintCandidateEvaluation candidateEvaluation = evaluate(variable, candidate, context);
				mutable.CandidateEvaluations++;
				if (!candidateEvaluation.Feasible)
					continue;
				bool preferred = preferredCandidates != null && preferredCandidates.TryGetValue(variable, out TCandidate preferredCandidate) && EqualityComparer<TCandidate>.Default.Equals(candidate, preferredCandidate);
				choices.Add(new BeamFrontier<TVariable, TCandidate>.Choice(candidate, candidateIndex, candidateEvaluation.Score, preferred));
			}

			if (mutable.BudgetExhausted)
				break;
			if (choices.Count == 0)
			{
				mutable.DeadStates++;
				mutable.LastDeadVariable = variable;
				continue;
			}

			BeamFrontier<TVariable, TCandidate>.SortChoices(choices);
			int take = Math.Min(candidateFanout, choices.Count);
			for (int choiceIndex = 0; choiceIndex < take; choiceIndex++)
			{
				BeamFrontier<TVariable, TCandidate>.Choice choice = choices[choiceIndex];
				Dictionary<TVariable, TCandidate> assigned = new(state.Assigned)
				{
					[variable] = choice.Candidate
				};
				uint hash = unchecked((state.DiversityHash ^ (uint)(choice.OriginalIndex + 1)) * 16777619u);
				long totalScore = state.Score + choice.Score + (choice.Preferred ? 32 : 0);
				expanded.Add(new BeamFrontier<TVariable, TCandidate>.State(assigned, totalScore, state.PreferredMatches + (choice.Preferred ? 1 : 0), hash));
				mutable.ExpandedStates++;
			}
		}
	}
}
