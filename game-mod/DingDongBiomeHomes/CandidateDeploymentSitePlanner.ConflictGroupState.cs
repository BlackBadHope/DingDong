using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConflictGroupRepairSearch
	{
		private readonly HashSet<string> moving;
		private readonly int baselineConflicts;
		private readonly Dictionary<string, AuthoredSceneMutationMask> masks;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools;
		private readonly List<string> variables;
		private readonly Dictionary<string, AuthoredDeploymentProfile> profileByNpc;
		private readonly Dictionary<RankedLayoutCandidate, int> candidateIds;
		private readonly CandidatePairCompatibilityCache hardPairCompatibilityCache;
		private readonly Dictionary<string, RankedLayoutCandidate> working;
		private long localPairChecks;
		private bool budgetExhausted;
		private int localNodes;
		private int blockerExpansions;
		private int attemptedVariables;
		private int attemptedDomains;

		public ConflictGroupRepairSearch(IReadOnlyList<string> conflictVariables, int baselineConflicts, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> working)
		{
			moving = new HashSet<string>(conflictVariables, StringComparer.Ordinal);
			this.baselineConflicts = baselineConflicts;
			this.masks = masks;
			this.unrestrictedPools = unrestrictedPools;
			this.variables = variables;
			this.profileByNpc = profileByNpc;
			this.candidateIds = candidateIds;
			this.hardPairCompatibilityCache = hardPairCompatibilityCache;
			this.working = working;
			attemptedVariables = moving.Count;
		}
	}
}
