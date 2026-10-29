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
	private const int ConstraintCollectorLimit = 384;
	private const int ConstraintCollectorCandidatesPerMacroBucket = 16;
	private const int ConstraintCandidatePoolLimit = 192;
	private const int ConstraintCandidatesPerSpatialBucket = 8;
	private const int ConstraintExactNodeLimit = 100000;
	private const int ConstraintSeededExactNodeLimit = 5000;
	private const int ConstraintSeededExactFinalNodeLimit = 10000;
	private const int ConstraintSeededExactRelaxationLimit = 24;
	private const int ConstraintRepairSectorExactNodeLimit = 10000;
	private const int ConstraintUnrestrictedExactNodeLimit = 50000;
	private const int ConstraintUnrestrictedExactVariableLimit = 24;
	private const int ConstraintUnrestrictedExactDomainValueLimit = 3000;
	private const int ConstraintExactNodesPerSectorPlan = 5000;
	private const int ConstraintLayerExactTimeLimitMilliseconds = 2500;
	private const int ConstraintExactPassTimeLimitMilliseconds = 2500;
	private const int ConstraintExactTotalTimeLimitMilliseconds = 15000;
	private const int ConstraintExactArcPairEvaluationLimit = 30000000;
	private const int ConstraintExactForwardPairEvaluationLimit = 30000000;
	private const int ConstraintExactCandidateEvaluationLimit = 1000000;
	private const int ConstraintRepairRestartLimit = 2;
	private const int ConstraintSurfaceRepairRestartLimit = 6;
	private const int ConstraintSurfaceRepairStepsPerRestart = 2000;
	private const int ConstraintRepairNeighborhoodRestartLimit = 2;
	// The first three unchanged rounds identify the minimal same-layer/sector
	// boundary that must be released. Keep one additional round so the solver
	// actually gets to search that expanded neighborhood.
	private const int ConstraintRepairNeighborhoodRoundLimit = 4;
	private const int ConstraintRepairStepsPerRestart = 1200;
	private const int ConstraintRepairPairEvaluationLimit = 60000000;
	private const int ConstraintRepairCandidateEvaluationLimit = 2000000;
	private const int ConstraintLayerRepairTimeLimitMilliseconds = 2000;
	private const int ConstraintRepairTimeLimitMilliseconds = 5000;
	private const int ConstraintRepairNeighborhoodTimeLimitMilliseconds = 2000;
	private const int ConstraintHardSafetyFallbackTimeLimitMilliseconds = 90000;
	private const int ConstraintHardSafetyFallbackRestartLimit = 8;
	private const int ConstraintBeamWidth = 64;
	private const int ConstraintBeamCandidateFanout = 16;
	private const int ConstraintBeamStatesPerDiversityBucket = 4;
	private const int ConstraintBeamPairEvaluationLimit = 20000000;
	private const int ConstraintBeamCandidateEvaluationLimit = 500000;
	private const int ConstraintBeamSectorPlanLimit = 1;
	private const int ConstraintSectorAllocationNodeLimit = 500000;
	private const int ConstraintSectorSupportPairEvaluationLimit = 20000000;
	// The downstream exact gate already maintains arc consistency while it
	// searches for a complete witness. A separate all-pairs leaf prepass burned
	// millions of comparisons and then discarded its work.
	private const int ConstraintSectorArcPairEvaluationLimit = 0;
	private const int ConstraintSectorDownstreamNodeLimit = 50000;
	private const int ConstraintSectorDownstreamPairEvaluationLimit = 10000000;
	private const int ConstraintSectorDownstreamCandidateEvaluationLimit = 250000;
	private const int ConstraintSectorDownstreamTimeLimitMilliseconds = 4000;
	private const int ConstraintSectorDownstreamTotalTimeLimitMilliseconds = 60000;
	private const int ConstraintSurfaceLayerRepairTimeLimitMilliseconds = 20000;
	private const int ConstraintSurfaceLayerExactTimeLimitMilliseconds = 30000;
	private const int ConstraintHardSafetyCoordinatedPairCheckLimit = 60000000;
	private const int ConstraintHardSafetyConflictGroupVariableLimit = 16;
	private const int ConstraintHardSafetyConflictGroupNodeLimit = 1000000;
	private const int ConstraintHardSafetyPortCohortDomainLimit = 96;
	private const int ConstraintHardSafetyConflictGroupBlockerExpansionLimit = 8;
}
