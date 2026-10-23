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
	private static bool SearchPortCohort(IReadOnlyDictionary<string,
		List<RankedLayoutCandidate>> remaining,
		Dictionary<string, AuthoredSceneMutationMask> masks,
		Dictionary<RankedLayoutCandidate, int> candidateIds,
		CandidatePairCompatibilityCache hardPairCompatibilityCache,
		Dictionary<string, RankedLayoutCandidate> assigned,
		IReadOnlyDictionary<string, RankedLayoutCandidate> fixedAssignments,
		List<string> variables, ref int nodes, ref long pairChecks,
		ref bool budgetExhausted, long deadlineTick)
	{
		if (++nodes > ConstraintHardSafetyConflictGroupNodeLimit || pairChecks > ConstraintHardSafetyCoordinatedPairCheckLimit || Environment.TickCount64 >= deadlineTick)
		{
			budgetExhausted = true;
			return false;
		}

		if (remaining.Count == 0)
			return AcceptPortCohort(assigned, fixedAssignments, variables, masks);
		KeyValuePair<string, List<RankedLayoutCandidate>> next = remaining.OrderBy(pair => pair.Value.Count).ThenBy(pair => pair.Key, StringComparer.Ordinal).First();
		foreach (RankedLayoutCandidate candidate in next.Value)
		{
			if (Environment.TickCount64 >= deadlineTick)
			{
				budgetExhausted = true;
				return false;
			}
			List<CandidateDeploymentSite> partial = PortCohortSites(
				fixedAssignments, assigned);
			AuthoredResidencePlacementContract contract =
				AuthoredResidencePlacementManifest.Required(next.Key);
			if (!AuthoredResidenceLayoutPolicy.TryValidateAggregateCapacity(
				contract, candidate.Site, partial, out _)) continue;
			bool compatible = true;
			foreach (KeyValuePair<string, RankedLayoutCandidate> existing in assigned)
			{
				if (++pairChecks > ConstraintHardSafetyCoordinatedPairCheckLimit)
				{
					budgetExhausted = true;
					return false;
				}

				if (!EvaluateHardConstraintPairCached(next.Key, candidate, existing.Key, existing.Value, masks, candidateIds, hardPairCompatibilityCache).Compatible)
				{
					compatible = false;
					break;
				}
			}

			if (!compatible)
				continue;
			Dictionary<string, List<RankedLayoutCandidate>> forwarded = new(StringComparer.Ordinal);
			foreach (KeyValuePair<string, List<RankedLayoutCandidate>> other in remaining)
			{
				if (string.Equals(other.Key, next.Key, StringComparison.Ordinal))
					continue;
				List<RankedLayoutCandidate> filtered = new();
				foreach (RankedLayoutCandidate otherCandidate in other.Value)
				{
					if (++pairChecks > ConstraintHardSafetyCoordinatedPairCheckLimit)
					{
						budgetExhausted = true;
						return false;
					}

					if (EvaluateHardConstraintPairCached(next.Key, candidate, other.Key, otherCandidate, masks, candidateIds, hardPairCompatibilityCache).Compatible)
						filtered.Add(otherCandidate);
				}

				if (filtered.Count == 0)
				{
					compatible = false;
					break;
				}

				forwarded.Add(other.Key, filtered);
			}

			if (!compatible)
				continue;
			assigned.Add(next.Key, candidate);
			if (SearchPortCohort(forwarded, masks, candidateIds,
				hardPairCompatibilityCache, assigned, fixedAssignments,
				variables, ref nodes, ref pairChecks,
				ref budgetExhausted, deadlineTick))
				return true;
			assigned.Remove(next.Key);
		}

		return false;
	}
}
