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
	public static bool TryPlanAll(Mod mod, IReadOnlyList<AuthoredDeploymentProfile> profiles, out List<CandidateDeploymentSite> sites, out string failure)
	{
		sites = new List<CandidateDeploymentSite>();
		foreach (AuthoredDeploymentProfile profile in profiles)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out AuthoredPreset preset))
			{
				failure = $"candidate-not-found:{profile.NpcKey}";
				return false;
			}

			AuthoredSceneMutationMask mutationMask = DingDongAuthoredStructureLoader.InspectMutationMask(mod, preset, profile);
			if (mutationMask.MutationCellCount <= 0)
			{
				failure = $"mutation-mask-empty:{profile.NpcKey}/{profile.PresetId}";
				return false;
			}

			// 3F is a one-scene-at-a-time visual gate. Simultaneous district
			// reservations belong to the later city-layout acceptance stage.
			List<Rectangle> independentSceneReservations = new();
			if (!TryPlan(profile, preset, mutationMask, independentSceneReservations, Array.Empty<CandidateDeploymentSite>(), null, surfaceIndex: null, layoutSalt: 0, allowPersistentAdvisory: true, out CandidateDeploymentSite site, out string reason))
			{
				failure = $"{profile.NpcKey}/{profile.PresetId}:{reason}";
				return false;
			}

			sites.Add(site);
		}

		failure = string.Empty;
		return true;
	}
}
