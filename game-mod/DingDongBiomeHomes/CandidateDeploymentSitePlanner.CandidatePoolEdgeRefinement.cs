using System;
using System.Collections.Generic;
using DingDongBiomeHomes.Worldgen;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConstraintCandidatePoolBuilder
	{
		private const int EdgeSupportPairCheckLimit = 250000;
		private const int EdgeSupportProfileLimit = 8;
		private const int EdgeSupportSamplesPerProfile = 8192;
		private const int EdgeSupportSampleLimit = 32768;
		private string edgeRefinementSummary = "edgeRefinement=not-run";

		private void RefineUnsupportedSurfacePairs(Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools)
		{
			if (Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive || surfaceIndex == null)
			{
				edgeRefinementSummary = "edgeRefinement=not-native-indexed";
				return;
			}
			bool Eligible(int index) => profiles[index].TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope;
			List<(int Left, int Right, long Product)> pairs = new();
			for (int left = 0; left < profiles.Count; left++)
				for (int right = left + 1; right < profiles.Count; right++)
					if ((Eligible(left) || Eligible(right)) &&
						pools.TryGetValue(profiles[left].NpcKey, out var a) && a.Count > 0 &&
						pools.TryGetValue(profiles[right].NpcKey, out var b) && b.Count > 0)
						pairs.Add((left, right, (long)a.Count * b.Count));
			pairs.Sort(static (a, b) => a.Product != b.Product ? a.Product.CompareTo(b.Product) :
				a.Left != b.Left ? a.Left.CompareTo(b.Left) : a.Right.CompareTo(b.Right));
			BoundedRefinementBudget checks = new(EdgeSupportPairCheckLimit);
			List<int> targets = new();
			HashSet<int> selected = new();
			int unsupported = 0, examined = 0, unknown = 0, skippedTargets = 0;
			foreach (var pair in pairs)
			{
				if (checks.Remaining == 0) break;
				examined++;
				// ProtectedArea disjointness is a mandatory hard solver condition.
				// Exhaustively failing this cheap necessary condition proves this
				// finite pair has no support. A disjoint witness is NOT advertised
				// as full solver compatibility; it merely avoids extra scanning.
				var support = CandidatePairSupportRefinement.Check(
					pools[profiles[pair.Left].NpcKey], pools[profiles[pair.Right].NpcKey],
					static (a, b) => a.Site.ProtectedArea.Intersects(b.Site.ProtectedArea), checks);
				if (support == CandidatePairSupportRefinement.Support.Unknown) { unknown++; continue; }
				if (support != CandidatePairSupportRefinement.Support.Unsupported) continue;
				unsupported++;
				mod.Logger.Info($"DINGDONG_PLANNER_EDGE_PAIR|left={profiles[pair.Left].NpcKey}|right={profiles[pair.Right].NpcKey}|state=NO_PROTECTED_SUPPORT|combinations={pair.Product}");
				Select(pair.Left);
				Select(pair.Right);
			}

			int sampleRemaining = EdgeSupportSampleLimit, expanded = 0, added = 0;
			foreach (int index in targets)
			{
				if (sampleRemaining == 0) break;
				var profile = profiles[index];
				var old = pools[profile.NpcKey];
				Dictionary<(int X, int Y, HomeBiome Biome), RankedLayoutCandidate> unique = new();
				HashSet<(int X, int Y, HomeBiome Biome)> existing = new();
				foreach (var candidate in old) existing.Add(CandidateKey(candidate));
				BoundedRefinementBudget samples = new(Math.Min(EdgeSupportSamplesPerProfile, sampleRemaining));
				string reason = "edge-contact-domain-empty";
				// One salt: the contacts use actual surface floors, not randomized
				// Y offsets. The edge scan visits coarse plus remaining columns.
				CollectSaltCandidates(profile, 1, unique, ref reason, samples, existing);
				sampleRemaining -= samples.Used;
				List<RankedLayoutCandidate> ordered = new(unique.Values);
				SortPoolCandidates(ordered);
				var merged = CandidatePairSupportRefinement.AppendPreserving(old,
					PruneSpatialBuckets(profile, ordered), CandidateKey, ConstraintCandidatePoolLimit);
				pools[profile.NpcKey] = merged;
				expanded++;
				added += merged.Count - old.Count;
				mod.Logger.Info($"DINGDONG_PLANNER_EDGE_REFINEMENT|npc={profile.NpcKey}|before={old.Count}|after={merged.Count}|added={merged.Count - old.Count}|samples={samples.Used}/{samples.Limit}|budgetReached={samples.Remaining == 0}|reason={reason}|oldCandidatesPreserved=true");
				if (activeCandidateDomainAudit != null)
				{
					var counts = activeCandidateDomainAudit.Profiles[profile.NpcKey].Counts;
					CandidateDomainAudit.Increment(counts, "pair-edge:profiles");
					CandidateDomainAudit.Increment(counts, "pair-edge:samples", samples.Used);
					CandidateDomainAudit.Increment(counts, "pair-edge:added", merged.Count - old.Count);
					if (samples.Remaining == 0) CandidateDomainAudit.Increment(counts, "pair-edge:budget-reached");
				}
			}
			edgeRefinementSummary = $"edgeRefinement=bounded-protected-support|edgePairs={examined}/{pairs.Count}|edgeUnsupported={unsupported}|edgeUnknown={unknown}|edgeChecks={checks.Used}/{checks.Limit}|edgeProfiles={expanded}/{targets.Count}|edgeSkippedTargets={skippedTargets}|edgeAdded={added}|edgeSamples={EdgeSupportSampleLimit - sampleRemaining}/{EdgeSupportSampleLimit}";
			mod.Logger.Info("DINGDONG_PLANNER_EDGE_SUMMARY|" + edgeRefinementSummary);

			void Select(int index)
			{
				if (!Eligible(index) || selected.Contains(index)) return;
				selected.Add(index);
				if (targets.Count >= EdgeSupportProfileLimit || pools[profiles[index].NpcKey].Count >= ConstraintCandidatePoolLimit)
				{ skippedTargets++; return; }
				targets.Add(index);
			}
		}

		private static (int X, int Y, HomeBiome Biome) CandidateKey(RankedLayoutCandidate candidate) =>
			(candidate.Site.AssetArea.X, candidate.Site.AssetArea.Y, candidate.Site.Biome);
	}
}
