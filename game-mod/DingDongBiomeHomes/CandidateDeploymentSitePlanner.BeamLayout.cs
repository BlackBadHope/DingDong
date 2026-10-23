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
	private static bool TryBeamLayout(List<AuthoredDeploymentProfile> ordered, Dictionary<string, AuthoredPreset> presets, Dictionary<string, AuthoredSceneMutationMask> masks, SurfaceColumnIndex surfaceIndex, int layoutSalt, List<string> attemptFailures, out List<CandidateDeploymentSite> planned)
	{
		List<LayoutBeamState> states = new()
		{
			new LayoutBeamState(new List<CandidateDeploymentSite>(), new List<Rectangle>(), 0)
		};
		for (int index = 0; index < ordered.Count; index++)
		{
			AuthoredDeploymentProfile profile = ordered[index];
			List<LayoutBeamState> next = new();
			string lastReason = "layout-not-found";
			foreach (LayoutBeamState state in states)
			{
				RankedCandidateCollector collector = RankedCandidateCollector.RuntimeBeam();
				List<(CandidateDeploymentSite Site, int Score)> bucket = collector.Items;
				CandidateDeploymentSite fallbackSite;
				bool found;
				ActiveRankedCollector = collector;
				try
				{
					found = TryPlan(profile, presets[profile.NpcKey], masks[profile.NpcKey], state.Reserved, state.Planned, masks, surfaceIndex, layoutSalt, allowPersistentAdvisory: true, out fallbackSite, out lastReason);
				}
				finally
				{
					ActiveRankedCollector = null;
				}

				if (bucket.Count == 0 && found && fallbackSite != null)
					bucket.Add((fallbackSite, 0));
				int fanout = Math.Min(LayoutBeamCandidateFanout, bucket.Count);
				for (int candidateIndex = 0; candidateIndex < fanout; candidateIndex++)
				{
					(CandidateDeploymentSite pick, int score) = bucket[candidateIndex];
					if (!ContainsOnlyRelocatableChestCollisions(pick, out lastReason))
						continue;
					List<CandidateDeploymentSite> expandedPlanned = new(state.Planned)
					{
						pick
					};
					List<Rectangle> expandedReserved = new(state.Reserved)
					{
						pick.ProtectedArea
					};
					next.Add(new LayoutBeamState(expandedPlanned, expandedReserved, state.Score + score));
				}
			}

			if (next.Count == 0)
			{
				attemptFailures.Add($"attempt{layoutSalt}:{profile.NpcKey}/{profile.PresetId}:{lastReason}:planned={index}/{ordered.Count}:beamStates={states.Count}:fanout={LayoutBeamCandidateFanout}");
				planned = new List<CandidateDeploymentSite>();
				return false;
			}

			states = PruneBeam(next);
		}

		planned = states[0].Planned;
		return planned.Count == ordered.Count;
	}

	private static List<LayoutBeamState> PruneBeam(List<LayoutBeamState> candidates)
	{
		candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
		Dictionary<(int Sector, int Layer, int XBucket, int YBucket), int> perBucket = new();
		List<LayoutBeamState> kept = new();
		foreach (LayoutBeamState candidate in candidates)
		{
			CandidateDeploymentSite last = candidate.Planned[candidate.Planned.Count - 1];
			AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(last.NpcKey);
			int yBucket = contract.BroadLayer == BroadLayoutLayer.Sky ? last.AssetArea.Y / 64 : 0;
			var key = (AuthoredResidenceLayoutPolicy.SectorOf(last.AssetArea.Center.X), (int)contract.BroadLayer, last.AssetArea.Center.X / 192, yBucket);
			perBucket.TryGetValue(key, out int count);
			if (count >= LayoutBeamStatesPerBucket)
				continue;
			perBucket[key] = count + 1;
			kept.Add(candidate);
			if (kept.Count >= LayoutBeamWidth)
				break;
		}

		return kept;
	}

	private sealed class LayoutBeamState
	{
		public List<CandidateDeploymentSite> Planned { get; }
		public List<Rectangle> Reserved { get; }
		public long Score { get; }

		public LayoutBeamState(List<CandidateDeploymentSite> planned, List<Rectangle> reserved, long score)
		{
			Planned = planned;
			Reserved = reserved;
			Score = score;
		}
	}
}
