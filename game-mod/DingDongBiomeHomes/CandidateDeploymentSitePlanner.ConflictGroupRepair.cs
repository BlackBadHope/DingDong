using System;
using System.Collections.Generic;
using System.Linq;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConflictGroupRepairSearch
	{
		public bool TryRepair(out int attemptedVariableCount, out int attemptedDomainCount, out int visitedNodes, out long evaluatedPairs)
		{
			if (moving.Count < 2 || moving.Count > ConstraintHardSafetyConflictGroupVariableLimit)
				return Complete(false, out attemptedVariableCount, out attemptedDomainCount, out visitedNodes, out evaluatedPairs);

			while (true)
			{
				List<string> movingVariables = new(moving);
				movingVariables.Sort(StringComparer.Ordinal);
				HashSet<string> portVariables = AuthoredPortVariables();
				bool balancePort = portVariables.Count > 0 && portVariables.All(moving.Contains);
				Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> domains = new(StringComparer.Ordinal);
				Dictionary<string, int> blockerCounts = new(StringComparer.Ordinal);
				bool emptyDomain = !TryBuildDomains(movingVariables, portVariables, balancePort, domains, blockerCounts);
				if (!budgetExhausted && !emptyDomain && TrySearchDomains(movingVariables, domains))
					return Complete(true, out attemptedVariableCount, out attemptedDomainCount, out visitedNodes, out evaluatedPairs);

				if (budgetExhausted || moving.Count >= ConstraintHardSafetyConflictGroupVariableLimit || blockerExpansions >= ConstraintHardSafetyConflictGroupBlockerExpansionLimit)
					return Complete(false, out attemptedVariableCount, out attemptedDomainCount, out visitedNodes, out evaluatedPairs);

				string blocker = blockerCounts.Where(pair => !moving.Contains(pair.Key)).OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key).FirstOrDefault();
				if (blocker == null)
					return Complete(false, out attemptedVariableCount, out attemptedDomainCount, out visitedNodes, out evaluatedPairs);
				moving.Add(blocker);
				blockerExpansions++;
				attemptedVariables = Math.Max(attemptedVariables, moving.Count);
			}
		}

		private bool Complete(bool solved, out int attemptedVariableCount, out int attemptedDomainCount, out int visitedNodes, out long evaluatedPairs)
		{
			attemptedVariableCount = attemptedVariables;
			attemptedDomainCount = attemptedDomains;
			visitedNodes = localNodes;
			evaluatedPairs = localPairChecks;
			return solved;
		}

		private HashSet<string> AuthoredPortVariables()
		{
			HashSet<string> ports = new(StringComparer.Ordinal);
			foreach (string variable in variables)
			{
				if (IsPortCohortVariable(variable))
					ports.Add(variable);
			}
			return ports;
		}
	}
}
