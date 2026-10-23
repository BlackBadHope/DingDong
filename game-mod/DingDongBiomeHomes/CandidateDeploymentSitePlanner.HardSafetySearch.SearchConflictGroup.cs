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
	private static bool SearchHardSafetyConflictGroup(IReadOnlyDictionary<string, List<RankedLayoutCandidate>> remainingDomains, Dictionary<string, AuthoredSceneMutationMask> masks, List<string> variables, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> working, int baselineConflicts, ref long localPairChecks, ref bool budgetExhausted, ref int localNodes, Dictionary<string, RankedLayoutCandidate> assigned, Dictionary<string, RankedLayoutCandidate> original)
	{
		if (++localNodes > ConstraintHardSafetyConflictGroupNodeLimit || localPairChecks > ConstraintHardSafetyCoordinatedPairCheckLimit)
		{
			budgetExhausted = true;
			return false;
		}

		if (remainingDomains.Count == 0)
		{
			foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in assigned)
				working[assignment.Key] = assignment.Value;
			if (CountConflicts(out _, masks, variables, candidateIds, hardPairCompatibilityCache, working) < baselineConflicts)
				return true;
			foreach (KeyValuePair<string, RankedLayoutCandidate> assignment in original)
				working[assignment.Key] = assignment.Value;
			return false;
		}

		string nextVariable = null;
		List<RankedLayoutCandidate> nextDomain = null;
		foreach (KeyValuePair<string, List<RankedLayoutCandidate>> remaining in remainingDomains)
		{
			if (nextDomain == null || remaining.Value.Count < nextDomain.Count || remaining.Value.Count == nextDomain.Count && string.Compare(remaining.Key, nextVariable, StringComparison.Ordinal) < 0)
			{
				nextVariable = remaining.Key;
				nextDomain = remaining.Value;
			}
		}

		foreach (RankedLayoutCandidate candidate in nextDomain)
		{
			Dictionary<string, List<RankedLayoutCandidate>> forwardedDomains = new(StringComparer.Ordinal);
			bool forwardFeasible = true;
			foreach (KeyValuePair<string, List<RankedLayoutCandidate>> remaining in remainingDomains)
			{
				if (string.Equals(remaining.Key, nextVariable, StringComparison.Ordinal))
					continue;
				List<RankedLayoutCandidate> compatibleDomain = new();
				foreach (RankedLayoutCandidate otherCandidate in remaining.Value)
				{
					if (++localPairChecks > ConstraintHardSafetyCoordinatedPairCheckLimit)
					{
						budgetExhausted = true;
						return false;
					}

					if (EvaluateHardConstraintPairCached(nextVariable, candidate, remaining.Key, otherCandidate, masks, candidateIds, hardPairCompatibilityCache).Compatible)
					{
						compatibleDomain.Add(otherCandidate);
					}
				}

				if (compatibleDomain.Count == 0)
				{
					forwardFeasible = false;
					break;
				}

				forwardedDomains.Add(remaining.Key, compatibleDomain);
			}

			if (!forwardFeasible)
				continue;
			assigned.Add(nextVariable, candidate);
			if (SearchHardSafetyConflictGroup(forwardedDomains, masks, variables, candidateIds, hardPairCompatibilityCache, working, baselineConflicts, ref localPairChecks, ref budgetExhausted, ref localNodes, assigned, original))
				return true;
			assigned.Remove(nextVariable);
		}

		return false;
	}
}
