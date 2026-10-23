using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintBeamSolver
{
	public static bool TrySelfTest(out string failure)
	{
		string[] variables =
		{
			"a",
			"b",
			"c",
			"d"
		};
		Dictionary<string, IReadOnlyList<int>> domains = new();
		Dictionary<string, int> preferred = new();
		foreach (string variable in variables)
		{
			domains.Add(variable, new[] { 1, 2, 3, 4 });
			preferred.Add(variable, 1);
		}

		bool solved = TrySolve(variables, domains, assigned => new Dictionary<string, int>(assigned), (_, candidate, _) => ConstraintCandidateEvaluation.Accept(10 - candidate), (_, candidate, _, other) => new ConstraintPairCompatibilityEvaluation(candidate != other, false), assigned => assigned.Count == variables.Length, preferred, 32, 4, 4, 10000, 1000, out List<KeyValuePair<string, int>> solution, out ConstraintBeamStats<string> stats);
		HashSet<int> values = new();
		foreach (KeyValuePair<string, int> assignment in solution)
			values.Add(assignment.Value);
		if (!solved || solution.Count != variables.Length || values.Count != variables.Length || stats.MaxDepth != variables.Length || stats.BudgetExhausted)
		{
			failure = $"beam:solved={solved}:solution={solution.Count}/{variables.Length}:unique={values.Count}:depth={stats.MaxDepth}:budget={stats.BudgetExhausted}";
			return false;
		}

		failure = string.Empty;
		return true;
	}
}
