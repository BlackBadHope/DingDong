using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConstraintCandidatePoolBuilder
	{
		private readonly Mod mod;
		private readonly IReadOnlyList<AuthoredDeploymentProfile> profiles;
		private readonly IReadOnlyDictionary<string, AuthoredPreset> presets;
		private readonly IReadOnlyDictionary<string, AuthoredSceneMutationMask> masks;
		private readonly SurfaceColumnIndex surfaceIndex;
		private int total;
		private int minimum = int.MaxValue;
		private int maximum;
		private int sectorDomainTotal;
		private int sectorDomainCount;
		private int sectorDomainMinimum = int.MaxValue;
		private int sectorDomainMaximum;
		private int structureMapRejected;
		private ulong poolDigest = 14695981039346656037UL;
		private readonly bool[] structureOnly;

		public ConstraintCandidatePoolBuilder(Mod mod, IReadOnlyList<AuthoredDeploymentProfile> profiles, IReadOnlyDictionary<string, AuthoredPreset> presets, IReadOnlyDictionary<string, AuthoredSceneMutationMask> masks, SurfaceColumnIndex surfaceIndex)
		{
			this.mod = mod;
			this.profiles = profiles;
			this.presets = presets;
			this.masks = masks;
			this.surfaceIndex = surfaceIndex;
			structureOnly = ContinentalPlacementMaskPolicy.CreateStructureOnly();
		}

		public bool TryBuild(out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools, out string summary, out string failure)
		{
			pools = new Dictionary<string, IReadOnlyList<RankedLayoutCandidate>>(StringComparer.Ordinal);
			List<string> emptyDomains = new();
			foreach (AuthoredDeploymentProfile profile in profiles)
			{
				if (!TryBuildProfilePool(profile, out List<RankedLayoutCandidate> pool, out string reason))
				{
					emptyDomains.Add($"{profile.NpcKey}/{profile.PresetId}:{reason}");
					continue;
				}
				pools.Add(profile.NpcKey, pool);
			}
			// Build the final digest/counts only after the one bounded refinement;
			// old candidates are retained verbatim, not pruned a second time.
			RefineUnsupportedSurfacePairs(pools);
			foreach (AuthoredDeploymentProfile profile in profiles)
				if (pools.TryGetValue(profile.NpcKey, out var finalPool))
					TrackProfilePool(profile, new List<RankedLayoutCandidate>(finalPool));
			if (emptyDomains.Count > 0)
			{
				summary = $"pools={pools.Count}/{profiles.Count}|empty={emptyDomains.Count}" +
					$"|total={total}|min={(minimum == int.MaxValue ? 0 : minimum)}|max={maximum}|{edgeRefinementSummary}";
				failure = $"candidate-domains-empty:count={emptyDomains.Count}|profiles=" +
					string.Join(";", emptyDomains);
				return false;
			}

			int sectorAverage = sectorDomainCount == 0 ? 0 : sectorDomainTotal / sectorDomainCount;
			summary = $"pools={pools.Count}/{profiles.Count}|total={total}|min={minimum}|max={maximum}|poolDigest={poolDigest:X16}|salts={CandidatePoolLayoutSalts}|collectorLimit={ConstraintCollectorLimit}|collectorPerSector={ConstraintCollectorCandidatesPerMacroBucket}|poolLimit={ConstraintCandidatePoolLimit}|structureMapRejected={structureMapRejected}|sectorDomainMin={(sectorDomainMinimum == int.MaxValue ? 0 : sectorDomainMinimum)}|sectorDomainAvg={sectorAverage}|sectorDomainMax={sectorDomainMaximum}|repairRestarts={ConstraintRepairRestartLimit}+{ConstraintRepairNeighborhoodRestartLimit}x{ConstraintRepairNeighborhoodRoundLimit}|repairStepsPerRestart={ConstraintRepairStepsPerRestart}|repairPairLimit={ConstraintRepairPairEvaluationLimit}|repairCandidateLimit={ConstraintRepairCandidateEvaluationLimit}|repairTimeLimitsMs={ConstraintLayerRepairTimeLimitMilliseconds}:{ConstraintRepairTimeLimitMilliseconds}:{ConstraintRepairNeighborhoodTimeLimitMilliseconds}";
			failure = string.Empty;
			summary += "|" + edgeRefinementSummary;
			return true;
		}
	}
}
