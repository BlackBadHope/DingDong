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
	private const int WorldMargin = 40;
	private const int BaseCoastalStrip = 780;
	private static int CoastalStripWidth =>
		Math.Max(BaseCoastalStrip, Math.Min(3200, Main.maxTilesX / 10));
	private const int SurfaceDistrictVerticalReach = 280;
	// Eight tiles on each translated visual footprint produce the intended
	// sixteen-tile settlement gap. Exact mutation masks remain non-overlapping.
	private const int VisualFocusClearance = 8;
	private const int SurfaceScanTop = 48;
	private const int SurfaceScanBelowLayer = 96;
	private const int CoastalTerrainEnvelopeMaximumRelief = 96;
	private static int CoastalTerrainEnvelopeMaximumOpenWaterGap =>
		Math.Max(96, Math.Min(320, Main.maxTilesX / 100));
	private const int JungleCanopyTerrainEnvelopeMaximumRelief = 160;
	private const int SnowLandmarkTerrainEnvelopeMaximumRelief = 320;
}
