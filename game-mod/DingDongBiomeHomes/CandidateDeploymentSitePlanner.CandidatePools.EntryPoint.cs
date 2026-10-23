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
	private static ResidencePoolReplayHints poolReplayHints;
	private static bool ReplayColumn(string npc, int x) => poolReplayHints?.ContainsColumn(npc, x) ?? true;
	private static bool ReplayArea(string npc, Rectangle area) => poolReplayHints?.ContainsArea(npc, area) ?? true;

	private static bool TryBuildConstraintCandidatePools(Mod mod, IReadOnlyList<AuthoredDeploymentProfile> profiles, IReadOnlyDictionary<string, AuthoredPreset> presets, IReadOnlyDictionary<string, AuthoredSceneMutationMask> masks, SurfaceColumnIndex surfaceIndex, out Dictionary<string, IReadOnlyList<RankedLayoutCandidate>> pools, out string summary, out string failure)
	{
		try
		{
			poolReplayHints = ResidencePoolReplayHints.Load();
			if (poolReplayHints != null)
				mod.Logger.Info("DINGDONG_POOL_REPLAY|scope=coordinate-hints|allAdmissionGates=enabled");
			return new ConstraintCandidatePoolBuilder(mod, profiles, presets, masks, surfaceIndex).TryBuild(out pools, out summary, out failure);
		}
		finally { poolReplayHints = null; }
	}
}
