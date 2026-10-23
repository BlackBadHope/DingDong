using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		private readonly AuthoredDeploymentProfile profile;
		private readonly AuthoredPreset preset;
		private readonly AuthoredSceneMutationMask mutationMask;
		private readonly List<Rectangle> reserved;
		private readonly IReadOnlyList<CandidateDeploymentSite> plannedSites;
		private readonly IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks;
		private readonly SurfaceColumnIndex surfaceIndex;
		private readonly int layoutSalt;
		private readonly bool allowPersistentAdvisory;
		private readonly AuthoredResidencePlacementContract contract;
		private readonly bool spreadLayout;
		private readonly List<(int Min, int Max)> indexedCenterRanges;
		private readonly List<(int Min, int Max)> ranges;
		private readonly int seed;
		private readonly Dictionary<string, int> unsupportedPersistentReasons = new(StringComparer.Ordinal);
		private int biomeRejected;
		private int boundsRejected;
		private int reservedRejected;
		private int persistentCandidates;
		private int unsupportedPersistentCandidates;
		private int protectedStructureRejected;
		private int terrainRejected;
		private int spacingRejected;
		private int independentRejected;
		private int megaRejected;
		private int densityRejected;
		private int massRejected;
		private int landmarkRejected;
		private CandidateDeploymentSite bestCleanSite;
		private int bestCleanScore = int.MinValue;
		private CandidateDeploymentSite bestAdvisorySite;
		private int bestPersistentOverlaps = int.MaxValue;
		private int bestAdvisoryScore = int.MinValue;

		public SurfacePlanningSearch(AuthoredDeploymentProfile profile, AuthoredPreset preset, AuthoredSceneMutationMask mutationMask, List<Rectangle> reserved, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, SurfaceColumnIndex surfaceIndex, int layoutSalt, bool allowPersistentAdvisory)
		{
			this.profile = profile;
			this.preset = preset;
			this.mutationMask = mutationMask;
			this.reserved = reserved;
			this.plannedSites = plannedSites;
			this.allMasks = allMasks;
			this.surfaceIndex = surfaceIndex;
			this.layoutSalt = layoutSalt;
			this.allowPersistentAdvisory = allowPersistentAdvisory;
			contract = AuthoredResidencePlacementManifest.Required(profile.NpcKey);
			spreadLayout = layoutSalt > 0;
			indexedCenterRanges = CollectHorizontalRanges(profile, contract, preset.Width, surfaceIndex);
			ranges = indexedCenterRanges;
			if (surfaceIndex != null && AuthoredSourceContext.HasRestoredReference(preset) &&
				profile.TargetAnchor != DeploymentTargetAnchor.CoastalWaterlineEnvelope)
			{
				// The index describes canvas-centre contacts. Keep the same physical
				// biome columns when the unchanged primary house gains left padding.
				// Keep the unindexed whole-world fallback unchanged and retain the
				// original centre ranges for separately projected edge contacts.
				int maximumLeft = Main.maxTilesX - 180 - preset.Width;
				ranges = new List<(int Min, int Max)>();
				foreach (var range in indexedCenterRanges)
				{
					if (range.Min == 180 && range.Max == maximumLeft)
						ranges.Add(range);
					else
						ranges.AddRange(Worldgen.SurfaceAnchorRangeProjection.Translate(
							new[] { range }, preset.Width, AuthoredSourceContext.ReferenceCenterOffsetX(preset),
							180, maximumLeft));
				}
			}
			// Exact waterline scenes (notably Sea King's restored galleon) are
			// admitted by their complete scene envelope. Translating narrow coastal
			// ranges by a reference-house offset can erase one coast and collapse the
			// other to a few columns, turning an otherwise recoverable ocean layout
			// into a global 61-residence failure.
			// Numeric Seed is the terrain-generating seed; SeedText may differ for secret worlds.
			seed = PositiveHash($"{profile.NpcKey}:{profile.PresetId}:{Main.ActiveWorldFileData?.Seed}:layout={layoutSalt}");
		}

		public bool TryFind(out CandidateDeploymentSite site, out string failure)
		{
			foreach ((int min, int max) in ranges)
			{
				if (TryScanRange(min, max, out site))
				{
					failure = string.Empty;
					return true;
				}
			}

			// A candidate rejected by StructureMap is not a successful coarse scan.
			bool empty = ActiveRankedCollector != null ? ActiveRankedCollector.Items.Count == 0 :
				bestCleanSite == null && (!allowPersistentAdvisory || bestAdvisorySite == null);
			if (spreadLayout && empty)
				foreach ((int min, int max) in ranges)
					if (TryScanRange(min, max, out site, refine: true))
					{
						failure = string.Empty;
						return true;
					}

			if (spreadLayout && ActiveRankedCollector != null && ActiveRankedCollector.Items.Count == 0 &&
				!Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive && surfaceIndex != null &&
				profile.TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope)
				ScanEdgeAnchorRanges();

			return FinishSearch(out site, out failure);
		}

		private bool FinishSearch(out CandidateDeploymentSite site, out string failure)
		{
			bool admitted = ActiveRankedCollector == null || ActiveRankedCollector.Items.Count > 0;
			if (admitted && bestCleanSite != null)
			{
				site = bestCleanSite;
				failure = string.Empty;
				return true;
			}
			if (admitted && allowPersistentAdvisory && bestAdvisorySite != null)
			{
				site = bestAdvisorySite;
				failure = string.Empty;
				return true;
			}

			string reasons = unsupportedPersistentReasons.Count == 0
				? "none"
				: string.Join(",", unsupportedPersistentReasons.OrderByDescending(entry => entry.Value).ThenBy(entry => entry.Key, StringComparer.Ordinal).Take(6).Select(entry => $"{entry.Key}x{entry.Value}"));
			return Fail($"surface-site-not-found:biome={biomeRejected},bounds={boundsRejected},reserved={reservedRejected},spacing={spacingRejected},independent={independentRejected},mega={megaRejected},density={densityRejected},mass={massRejected},landmarkAdj={landmarkRejected},persistentCandidates={persistentCandidates},unsupportedPersistentCandidates={unsupportedPersistentCandidates},unsupportedReasons={reasons},protectedStructure={protectedStructureRejected},terrain={terrainRejected},terrainDetails={TerrainDiagnostics}", out site, out failure);
		}
	}
}
