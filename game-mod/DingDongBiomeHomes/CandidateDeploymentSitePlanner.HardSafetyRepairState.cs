using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class HardSafetyRepairSession
	{
		private readonly IReadOnlyList<AuthoredDeploymentProfile> profiles;
		private readonly Dictionary<string, AuthoredSceneMutationMask> masks;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools;
		private readonly List<string> variables;
		private readonly Dictionary<string, AuthoredDeploymentProfile> profileByNpc;
		private readonly Dictionary<RankedLayoutCandidate, int> candidateIds;
		private readonly CandidatePairCompatibilityCache hardPairCompatibilityCache;
		private readonly long deadlineTick;
		private readonly Dictionary<string, RankedLayoutCandidate> working = new(StringComparer.Ordinal);
		private string portSeedTrace = "attempted=False";
		private int singleMoves;
		private int coordinatedSearches;
		private long coordinatedPairChecks;
		private int groupSearches;
		private int groupVariables;
		private int groupDomainValues;
		private int groupNodes;
		private long groupPairChecks;
		private int initialConflicts;
		public string SafetyFailure { get; private set; }
		public string SafetyReport { get; private set; }

		public HardSafetyRepairSession(IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache hardPairCompatibilityCache, long deadlineTick, string safetyFailure, string safetyReport)
		{
			this.profiles = profiles;
			this.masks = masks;
			this.unrestrictedPools = unrestrictedPools;
			this.variables = variables;
			this.profileByNpc = profileByNpc;
			this.candidateIds = candidateIds;
			this.hardPairCompatibilityCache = hardPairCompatibilityCache;
			this.deadlineTick = deadlineTick;
			SafetyFailure = safetyFailure;
			SafetyReport = safetyReport;
		}

		private bool TimeBudgetExhausted => Environment.TickCount64 >= deadlineTick;
	}
}
