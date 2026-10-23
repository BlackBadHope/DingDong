using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Candidate-envelope search shared by the independent 3F preview gate and
/// normal world generation. Independent planning intentionally ignores other
/// DingDong scenes. Simultaneous planning broad-phases with every accepted
/// protected footprint, keeps each primary authored residence focus visually
/// separate, resolves sparse-envelope overlaps against the exact mutation
/// masks, and rejects persistent-object collisions before any tile mutation.
/// </summary>
public static partial class CandidateDeploymentSitePlanner
{
	private static bool TrySeedPortCohort(out string trace, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> working, long deadlineTick)
	{
		List<string> portVariables = variables.Where(IsPortCohortVariable)
			.OrderBy(variable => variable, StringComparer.Ordinal).ToList();
		if (portVariables.Count < 2)
		{
			trace = $"attempted=False|variables={portVariables.Count}";
			return false;
		}

		Dictionary<string, RankedLayoutCandidate> fixedAssignments = new(StringComparer.Ordinal);
		foreach (string variable in variables)
			if (!portVariables.Contains(variable)) fixedAssignments.Add(variable, working[variable]);
		int nodes = 0;
		long pairChecks = 0;
		bool budgetExhausted = false;
		Dictionary<string, List<RankedLayoutCandidate>> domains = new(StringComparer.Ordinal);
		foreach (string variable in portVariables)
		{
			if (Environment.TickCount64 >= deadlineTick)
			{
				trace = $"attempted=True|solved=False|variables={portVariables.Count}|pairChecks={pairChecks}|budgetExhausted=True|reason=time";
				return false;
			}
			List<RankedLayoutCandidate> source = CompatibleWithFixed(variable,
				unrestrictedPools[variable], fixedAssignments, masks, candidateIds,
				hardPairCompatibilityCache, ref pairChecks, ref budgetExhausted);
			if (source.Count == 0 || budgetExhausted)
			{
				trace = $"attempted=True|solved=False|variables={portVariables.Count}" +
					$"|empty={variable}|pairChecks={pairChecks}" +
					$"|budgetExhausted={budgetExhausted}";
				return false;
			}

			List<RankedLayoutCandidate> balanced = new(ConstraintHardSafetyPortCohortDomainLimit);
			HashSet<(int Side, int Bucket)> seen = new();
			List<RankedLayoutCandidate> left = source.Where(candidate => candidate.Site.AssetArea.Center.X < Main.maxTilesX / 2).ToList();
			List<RankedLayoutCandidate> right = source.Where(candidate => candidate.Site.AssetArea.Center.X >= Main.maxTilesX / 2).ToList();
			if (source.Contains(working[variable]))
				AddSpatialCandidate(working[variable], balanced, seen);
			for (int index = 0; balanced.Count < ConstraintHardSafetyPortCohortDomainLimit && (index < left.Count || index < right.Count); index++)
			{
				if (index < left.Count)
					AddSpatialCandidate(left[index], balanced, seen);
				if (index < right.Count)
					AddSpatialCandidate(right[index], balanced, seen);
			}

			domains.Add(variable, balanced);
		}

		Dictionary<string, RankedLayoutCandidate> assigned = new(StringComparer.Ordinal);
		bool solved = SearchPortCohort(domains, masks, candidateIds,
			hardPairCompatibilityCache, assigned, fixedAssignments, variables,
			ref nodes, ref pairChecks, ref budgetExhausted, deadlineTick);
		if (solved)
		{
			foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in assigned)
				working[assignment.Key] = assignment.Value;
		}

		trace = $"attempted=True|solved={solved}|variables={portVariables.Count}|nodes={nodes}|pairChecks={pairChecks}|budgetExhausted={budgetExhausted}";
		return solved;
	}

	private static void AddSpatialCandidate(RankedLayoutCandidate candidate, List<RankedLayoutCandidate> balanced, HashSet<(int Side, int Bucket)> seen)
	{
		if (balanced.Count >= ConstraintHardSafetyPortCohortDomainLimit)
			return;
		int side = candidate.Site.AssetArea.Center.X < Main.maxTilesX / 2 ? 0 : 1;
		if (seen.Add((side, candidate.Site.AssetArea.Center.X / 16)))
			balanced.Add(candidate);
	}
}
