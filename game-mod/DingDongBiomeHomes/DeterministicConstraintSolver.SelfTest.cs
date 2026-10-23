using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintSolver
{
	public static bool TrySelfTest(out string failure)
	{
		failure = string.Empty;
		if (!TryVerifyArcMrvForwardCheck(ref failure))
			return false;
		if (!TryVerifyArcWipeoutDiagnostics(ref failure))
			return false;
		if (!TryVerifyAggregateForwardPruning(ref failure))
			return false;
		if (!TryVerifyLeastConstrainingOrder(ref failure))
			return false;
		if (!TryVerifyPreferredValueOrder(ref failure))
			return false;
		failure = string.Empty;
		return true;
	}

	private static bool TryVerifyPreferredValueOrder(ref string failure)
	{
		bool hintedSolved = TrySolve(new[] { "only" }, new Dictionary<string, IReadOnlyList<int>> { ["only"] = new[] { 1, 2 } }, _ => 0, (_, candidate, _) => ConstraintCandidateEvaluation.Accept(10 - candidate), (_, _, _) => true, (_, _, _, _) => new ConstraintPairCompatibilityEvaluation(true, false), _ => true, new Dictionary<string, int> { ["only"] = 2 }, false, false, 10, 10, 10, 1000, 10, out List<KeyValuePair<string, int>> hintedSolution, out _);
		if (!hintedSolved || hintedSolution.Count != 1 || hintedSolution[0].Value != 2)
		{
			failure = $"preferred-value-order:solved={hintedSolved}:value={(hintedSolution.Count == 0 ? 0 : hintedSolution[0].Value)}";
			return false;
		}

		return true;
	}
}
