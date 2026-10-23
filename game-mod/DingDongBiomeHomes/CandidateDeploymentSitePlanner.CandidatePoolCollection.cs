using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class ConstraintCandidatePoolBuilder
	{
		private bool TryBuildProfilePool(AuthoredDeploymentProfile profile, out List<RankedLayoutCandidate> pruned, out string lastReason)
		{
			Dictionary<(int X, int Y, HomeBiome Biome), RankedLayoutCandidate> unique = new();
			lastReason = "candidate-domain-empty";
			for (int layoutSalt = 1; layoutSalt <= CandidatePoolLayoutSalts; layoutSalt++)
				CollectSaltCandidates(profile, layoutSalt, unique, ref lastReason);

			List<RankedLayoutCandidate> ordered = new(unique.Values);
			SortPoolCandidates(ordered);
			pruned = PruneSpatialBuckets(profile, ordered);
			return pruned.Count > 0;
		}

		private static void SortPoolCandidates(List<RankedLayoutCandidate> ordered) =>
			ordered.Sort(static (left, right) =>
			{
				int score = right.BaseScore.CompareTo(left.BaseScore);
				if (score != 0)
					return score;
				int x = left.Site.AssetArea.X.CompareTo(right.Site.AssetArea.X);
				return x != 0 ? x : left.Site.AssetArea.Y.CompareTo(right.Site.AssetArea.Y);
			});
		private void CollectSaltCandidates(AuthoredDeploymentProfile profile, int layoutSalt, Dictionary<(int X, int Y, HomeBiome Biome), RankedLayoutCandidate> unique, ref string lastReason,
			Worldgen.BoundedRefinementBudget edgeBudget = null, HashSet<(int X, int Y, HomeBiome Biome)> existingGeometry = null)
		{
			int admissionRejected = 0;
			string lastAdmissionReason = "structure-map-collision";
			RankedCandidateCollector collector = RankedCandidateCollector.ConstraintPool(candidate =>
			{
				// Already admitted pool geometry cannot consume the bounded edge
				// collector's slots again. New geometry still passes every gate.
				if (existingGeometry != null && existingGeometry.Contains((candidate.AssetArea.X, candidate.AssetArea.Y, candidate.Biome))) return false;
				bool admitted = AuthoredResidenceStructureReservation.CanReserve(candidate, structureOnly, out string rejection);
				activeCandidateDomainAudit?.Admission(candidate, admitted, rejection);
				if (admitted)
					return true;
				lastAdmissionReason = rejection;
				admissionRejected++;
				structureMapRejected++;
				if (layoutSalt == 1 && admissionRejected <= 3)
					mod.Logger.Info($"DINGDONG_PLANNER_RESERVATION_REJECT|npc={profile.NpcKey}|reason={rejection}|asset={candidate.AssetArea}|protected={candidate.ProtectedArea}|conflicts={ResidenceReservationDiagnostics.Describe(candidate.ProtectedArea)}");
				return false;
			});
			List<(CandidateDeploymentSite Site, int Score)> ranked = collector.Items;
			ActiveRankedCollector = collector;
			CandidateDeploymentSite fallbackSite;
			bool found;
			try
			{
				if (edgeBudget == null)
					found = TryPlan(profile, presets[profile.NpcKey], masks[profile.NpcKey], new List<Rectangle>(), Array.Empty<CandidateDeploymentSite>(), masks, surfaceIndex, layoutSalt, allowPersistentAdvisory: true, out fallbackSite, out lastReason);
				else
				{
					var search = new SurfacePlanningSearch(profile, presets[profile.NpcKey], masks[profile.NpcKey], new List<Rectangle>(), Array.Empty<CandidateDeploymentSite>(), masks, surfaceIndex, layoutSalt, allowPersistentAdvisory: true);
					try { found = search.TryFindEdgeContacts(edgeBudget, out fallbackSite, out lastReason); }
					finally { search.CaptureCandidateDomainDiagnostics(); }
				}
			}
			finally
			{
				ActiveRankedCollector = null;
			}

			if (ranked.Count == 0 && layoutSalt == 1)
				mod.Logger.Info($"DINGDONG_PLANNER_EMPTY_DOMAIN|npc={profile.NpcKey}|terrain={lastReason}|reservationRejected={admissionRejected}");
			if (ranked.Count == 0 && admissionRejected > 0)
				lastReason = lastAdmissionReason;
			if (ranked.Count == 0 && found && fallbackSite != null)
			{
				if (AuthoredResidenceStructureReservation.CanReserve(fallbackSite, structureOnly, out string fallbackFailure))
					ranked.Add((fallbackSite, 0));
				else
				{
					structureMapRejected++;
					lastReason = fallbackFailure;
				}
			}
			foreach ((CandidateDeploymentSite candidate, int score) in ranked)
			{
				if (!AuthoredResidenceStructureReservation.CanReserve(candidate,
					structureOnly, out string admissionFailure))
				{
					structureMapRejected++;
					lastReason = admissionFailure;
					continue;
				}
				if (!ContainsOnlyRelocatableChestCollisions(candidate, out _))
					continue;
				var key = (candidate.AssetArea.X, candidate.AssetArea.Y, candidate.Biome);
				if (!unique.TryGetValue(key, out RankedLayoutCandidate existing) || score > existing.BaseScore)
					unique[key] = new RankedLayoutCandidate(candidate, score);
			}
		}

	}
}
