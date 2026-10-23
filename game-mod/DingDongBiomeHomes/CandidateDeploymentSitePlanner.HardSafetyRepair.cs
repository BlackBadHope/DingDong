using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class HardSafetyRepairSession
	{
		public bool TryRepair(IReadOnlyList<KeyValuePair<string, RankedLayoutCandidate>> seedAssignment, out List<KeyValuePair<string, RankedLayoutCandidate>> repairedSolution, out string trace)
		{
			repairedSolution = new List<KeyValuePair<string, RankedLayoutCandidate>>();
			foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in seedAssignment)
				working[assignment.Key] = assignment.Value;
			if (working.Count != variables.Count)
			{
				trace = $"attempted=True|solved=False|reason=incomplete-seed:{working.Count}/{variables.Count}";
				return false;
			}

			initialConflicts = CountConflicts(out List<(string Left, string Right)> initialPairs, masks, variables, candidateIds, hardPairCompatibilityCache, working);
			if (initialConflicts == 0 && TryComplete(repairedSolution, out trace))
			{
				portSeedTrace = "attempted=False|reason=complete-seed";
				return true;
			}
			if (TimeBudgetExhausted)
			{
				trace = $"{FormatTrace(solved: false, initialConflicts)}|reason=time-budget-exhausted";
				return false;
			}
			// Pair-clean aggregate failures (mass, mega sectors, layer coverage) belong
			// to the distribution solver. Port seeding only moves pair-conflicted port
			// variables and cannot repair those aggregate constraints; returning here
			// preserves the complete fallback deadline for the canonical solver.
			if (initialConflicts == 0)
			{
				trace = $"{FormatTrace(solved: false, finalConflicts: 0)}|reason={SafetyFailure}";
				return false;
			}
			for (int round = 0; round < 32; round++)
			{
				if (TimeBudgetExhausted)
					break;
				int conflictCount = CountConflicts(out List<(string Left, string Right)> conflicts, masks, variables, candidateIds, hardPairCompatibilityCache, working);
				if (conflictCount == 0)
					return TryComplete(repairedSolution, out trace);
				if (TryMoveSingle(conflicts, conflictCount) || TryMoveCoordinatedPair(conflicts, conflictCount) || TryMoveConflictGroup(conflicts, conflictCount))
					continue;
				if (ConflictsTouchPort(conflicts) && TrySeedPortCohort(out portSeedTrace))
					continue;
				break;
			}

			int finalConflicts = CountConflicts(out List<(string Left, string Right)> finalPairs, masks, variables, candidateIds, hardPairCompatibilityCache, working);
			string finalPair = finalPairs.Count == 0 ? "none" : DescribeFinalPairs(finalPairs);
			trace = $"{FormatTrace(solved: false, finalConflicts)}|finalPair={finalPair}";
			return false;
		}

		private bool TryComplete(List<KeyValuePair<string, RankedLayoutCandidate>> repairedSolution, out string trace)
		{
			string safetyFailure = SafetyFailure;
			string safetyReport = SafetyReport;
			if (!AcceptHardSafetyComplete(working, profiles, masks, variables, ref safetyFailure, ref safetyReport))
			{
				SafetyFailure = safetyFailure;
				SafetyReport = safetyReport;
				trace = $"{FormatTrace(solved: false, finalConflicts: 0)}|reason={SafetyFailure}";
				return false;
			}
			SafetyFailure = safetyFailure;
			SafetyReport = safetyReport;
			foreach (string variable in variables)
				repairedSolution.Add(new KeyValuePair<string, RankedLayoutCandidate>(variable, working[variable]));
			trace = FormatTrace(solved: true, finalConflicts: 0);
			return true;
		}

		private string FormatTrace(bool solved, int finalConflicts) =>
			$"attempted=True|solved={solved}|portSeed={portSeedTrace}|initialConflicts={initialConflicts}|finalConflicts={finalConflicts}|singleMoves={singleMoves}|coordinatedSearches={coordinatedSearches}|coordinatedPairChecks={coordinatedPairChecks}|groupSearches={groupSearches}|groupVariables={groupVariables}|groupDomains={groupDomainValues}|groupNodes={groupNodes}|groupPairChecks={groupPairChecks}";

		private string DescribeFinalPairs(List<(string Left, string Right)> pairs)
		{
			List<string> descriptions = pairs.ConvertAll(pair => DescribeFinalConflict(pair, masks, unrestrictedPools, candidateIds, hardPairCompatibilityCache, working));
			return string.Join(';', descriptions.GetRange(0, Math.Min(8, pairs.Count)));
		}

		private static bool ConflictsTouchPort(List<(string Left, string Right)> conflicts)
		{
			foreach ((string left, string right) in conflicts)
				if (IsPortCohortVariable(left) || IsPortCohortVariable(right))
					return true;
			return false;
		}

		private bool TrySeedPortCohort(out string trace) =>
			CandidateDeploymentSitePlanner.TrySeedPortCohort(out trace, masks, unrestrictedPools, variables, candidateIds, hardPairCompatibilityCache, working, deadlineTick);
	}
}
