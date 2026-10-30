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
	private const int SimultaneousLayoutAttempts = 8;
	private const int RankedCandidateLimit = 48;
	private const int RankedCandidatesPerMacroBucket = 2;
	private const int TightBiomeCandidatesPerMacroBucket = 6;
	private const int LayoutBeamWidth = 24;
	private const int LayoutBeamCandidateFanout = 12;
	private const int LayoutBeamStatesPerBucket = 3;
	private const int CandidatePoolLayoutSalts = 8;
	private const bool ParallelCandidatePools = true;

	// Owner's world 29.10 (seed 2097116013): the surface layer ended one pair short of a full plan
	// and the world got no residences at all. A failed plan is retried with another search key;
	// retry 0 keeps the historic key exactly, so worlds that already plan are unchanged.
	internal const int PlanningRetries = 5; // 30.10: the owner seed sits at the edge of surface capacity
	internal static int PlanningRetry;
	internal static string PlanningSeedKey => PlanningRetry == 0
		? $"{Main.ActiveWorldFileData?.Seed}" : $"{Main.ActiveWorldFileData?.Seed}#retry{PlanningRetry}";
}
