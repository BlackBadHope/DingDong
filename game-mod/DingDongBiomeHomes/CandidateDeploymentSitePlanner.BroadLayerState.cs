using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class BroadLayerSolverSession
	{
		private readonly Mod mod;
		private readonly IReadOnlyList<AuthoredDeploymentProfile> profiles;
		private readonly Dictionary<string, AuthoredPreset> presets;
		private readonly Dictionary<string, AuthoredSceneMutationMask> masks;
		private readonly IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools;
		private readonly List<string> variables;
		private readonly Dictionary<string, AuthoredDeploymentProfile> profileByNpc;
		private readonly Dictionary<string, Dictionary<string, int>> rejectionReasons;
		private readonly Dictionary<RankedLayoutCandidate, int> candidateIds;
		private readonly CandidatePairCompatibilityCache pairCompatibilityCache;
		private readonly CandidatePairCompatibilityCache hardPairCompatibilityCache;
		private readonly Dictionary<string, RankedLayoutCandidate> merged = new(StringComparer.Ordinal);
		private readonly List<string> layerTraces = new(4);
		private bool allLayersSolved = true;
		public string CompleteFailure { get; private set; }

		public BroadLayerSolverSession(Mod mod, IReadOnlyList<AuthoredDeploymentProfile> profiles, Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, IReadOnlyDictionary<string, IReadOnlyList<RankedLayoutCandidate>> unrestrictedPools, List<string> variables, Dictionary<string, AuthoredDeploymentProfile> profileByNpc, Dictionary<string, Dictionary<string, int>> rejectionReasons, Dictionary<RankedLayoutCandidate, int> candidateIds, CandidatePairCompatibilityCache pairCompatibilityCache, CandidatePairCompatibilityCache hardPairCompatibilityCache, string completeFailure)
		{
			this.mod = mod;
			this.profiles = profiles;
			this.presets = presets;
			this.masks = masks;
			this.unrestrictedPools = unrestrictedPools;
			this.variables = variables;
			this.profileByNpc = profileByNpc;
			this.rejectionReasons = rejectionReasons;
			this.candidateIds = candidateIds;
			this.pairCompatibilityCache = pairCompatibilityCache;
			this.hardPairCompatibilityCache = hardPairCompatibilityCache;
			CompleteFailure = completeFailure;
		}
	}

	private sealed class BroadLayerWork
	{
		public BroadLayoutLayer Layer;
		public List<string> Variables;
		public Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> Domains;
		public int DomainValues;
		public int RepairBudget;
		public int ExactBudget;
		public int RepairRestarts;
		public int RepairSteps;
		public long Started;
		public bool Solved;
		public string Method = "repair";
		public List<KeyValuePair<string, RankedLayoutCandidate>> Solution = new();
		public List<KeyValuePair<string, RankedLayoutCandidate>> CompleteSeed = new();
		public ConstraintRepairStats<string> RepairStats;
	}
}
