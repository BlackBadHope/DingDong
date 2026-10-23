using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintBeamSolver
{
	/// <summary>
	/// Owns the constructive search frontier and its deterministic ranking and
	/// diversity policy. Solver orchestration only advances this frontier.
	/// </summary>
	private sealed class BeamFrontier<TVariable, TCandidate>
		where TVariable : notnull
	{
		public readonly record struct Choice(TCandidate Candidate, int OriginalIndex, int Score, bool Preferred);
		public sealed record State(Dictionary<TVariable, TCandidate> Assigned, long Score, int PreferredMatches, uint DiversityHash);

		private List<State> states = new()
		{
			new State(new Dictionary<TVariable, TCandidate>(), 0, 0, 2166136261u)
		};

		public IReadOnlyList<State> States => states;
		public int Count => states.Count;

		public void Advance(List<State> expanded, int beamWidth, int statesPerDiversityBucket)
		{
			expanded.Sort(static (left, right) =>
			{
				int score = right.Score.CompareTo(left.Score);
				if (score != 0)
					return score;
				int preferred = right.PreferredMatches.CompareTo(left.PreferredMatches);
				return preferred != 0 ? preferred : left.DiversityHash.CompareTo(right.DiversityHash);
			});

			Dictionary<int, int> perBucket = new();
			states = new List<State>(Math.Min(beamWidth, expanded.Count));
			foreach (State state in expanded)
			{
				int bucket = (int)(state.DiversityHash & 63u);
				perBucket.TryGetValue(bucket, out int count);
				if (count >= statesPerDiversityBucket)
					continue;
				perBucket[bucket] = count + 1;
				states.Add(state);
				if (states.Count >= beamWidth)
					break;
			}
		}

		public static void SortChoices(List<Choice> choices)
		{
			choices.Sort(static (left, right) =>
			{
				int score = right.Score.CompareTo(left.Score);
				if (score != 0)
					return score;
				int preferred = right.Preferred.CompareTo(left.Preferred);
				return preferred != 0 ? preferred : left.OriginalIndex.CompareTo(right.OriginalIndex);
			});
		}
	}
}
