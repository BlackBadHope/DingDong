using System.Collections.Generic;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class CrossLayerRepairSession
	{
		private readonly IReadOnlyList<AuthoredDeploymentProfile> profiles;
		private readonly Dictionary<string, AuthoredPreset> presets;
		private readonly Dictionary<string, AuthoredSceneMutationMask> masks;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools;
		private readonly List<string> variables;
		private readonly Dictionary<string, AuthoredDeploymentProfile> profileByNpc;
		private readonly Dictionary<string, Dictionary<string, int>> rejectionReasons;
		private readonly Dictionary<RankedLayoutCandidate, int> candidateIds;
		private readonly CandidatePairCompatibilityCache pairCompatibilityCache;
		private readonly Dictionary<string, RankedLayoutCandidate> merged;
		public string CompleteFailure { get; private set; }

		public CrossLayerRepairSession(IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<string, Dictionary<string, int>> rejectionReasons, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache pairCompatibilityCache, Dictionary<string, RankedLayoutCandidate> merged, string completeFailure)
		{
			this.profiles = profiles;
			this.presets = presets;
			this.masks = masks;
			this.unrestrictedPools = unrestrictedPools;
			this.variables = variables;
			this.profileByNpc = profileByNpc;
			this.rejectionReasons = rejectionReasons;
			this.candidateIds = candidateIds;
			this.pairCompatibilityCache = pairCompatibilityCache;
			this.merged = merged;
			CompleteFailure = completeFailure;
		}
	}
}
