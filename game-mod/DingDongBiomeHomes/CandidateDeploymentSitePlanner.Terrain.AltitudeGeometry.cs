using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public static partial class CandidateDeploymentSitePlanner
{
	private static Rectangle AreaAt(AuthoredDeploymentProfile profile, AuthoredPreset preset, AuthoredSceneMutationMask mutationMask, int left, int floorY, int layoutSalt, ResidenceAltitudeBand band)
	{
		int top = profile.TargetAnchor switch
		{
			DeploymentTargetAnchor.SurfaceGroundContact => floorY - (preset.Height - 1 - preset.GroundOffset!.Value),
			DeploymentTargetAnchor.SurfaceTerrainEnvelope => floorY - (mutationMask.TerrainAnchorY ?? preset.Height * 3 / 5),
			DeploymentTargetAnchor.CoastalWaterlineEnvelope => floorY - (mutationMask.TerrainAnchorY ?? preset.Height / 2),
			DeploymentTargetAnchor.SkyClearanceEnvelope => SkyTop(profile, preset, left, layoutSalt, band),
			_ => throw new InvalidOperationException($"Surface area requested for unsupported anchor: {profile.TargetAnchor}.")};
		return new Rectangle(left, top, preset.Width, preset.Height);
	}

	private static int SkyTop(AuthoredDeploymentProfile profile, AuthoredPreset preset, int left, int layoutSalt, ResidenceAltitudeBand band)
	{
		int minTop = WorldMargin + profile.ProtectedPadding;
		int maxTop = Math.Max(minTop, (int)Main.worldSurface - preset.Height - 48);
		if (maxTop <= minTop)
			return minTop;
		int span = maxTop - minTop;
		int third = Math.Max(1, span / 3);
		int lo;
		int hi;
		switch (band)
		{
			case ResidenceAltitudeBand.HighSky:
				lo = minTop;
				hi = minTop + third;
				// Search the whole allowed high third. Lower positions are already
				// preferred by the -420 ceiling score, and the joint acceptance gate
				// allows at most two sites above y=110. Turning that preference into
				// a sampling cut silently removed legal clearance above coastal POIs.
				break;
			case ResidenceAltitudeBand.MidSky:
				lo = minTop + third;
				hi = minTop + 2 * third;
				break;
			case ResidenceAltitudeBand.LowSky:
				lo = minTop + 2 * third;
				hi = maxTop;
				break;
			default:
				lo = minTop;
				hi = maxTop;
				break;
		}

		if (hi < lo)
			hi = lo;
		int hash = PositiveHash($"{profile.NpcKey}:{layoutSalt}:{left / 48}:{band}");
		int width = Math.Max(1, hi - lo + 1);
		return lo + (hash % width);
	}

	private static int MaximumSurfaceRelief(ResidenceScaleClass scale) => scale switch
	{
		ResidenceScaleClass.Compact => 18,
		ResidenceScaleClass.Medium => 36,
		ResidenceScaleClass.Large => 64,
		ResidenceScaleClass.Landmark => 104,
		ResidenceScaleClass.Mega => 160,
		_ => 48
	};
	private static List<(ResidenceAltitudeBand Band, int Min, int Max)> CavernAltitudeRanges(AuthoredResidencePlacementContract contract, int height)
	{
		List<(ResidenceAltitudeBand Band, int Min, int Max)> ranges = new();
		// Surface terrain contact may legitimately sit up to 96 tiles below the
		// global layer marker, and large authored scenes can extend below that
		// contact. Keep the first underground envelope physically separate instead
		// of relying on a cross-layer collision exception at final acceptance.
		int undergroundMin = Math.Max((int)Main.worldSurface + 180, 420);
		int cavernMin = Math.Max((int)Main.rockLayer + 48, undergroundMin + 80);
		// Retain the Large 320-cell buffer above the actual biome boundary.
		// A taller Underworld must not overlap newly admitted deep residences.
		// Match the generator directly; callers of Terraria's trivial hooked
		// getter can otherwise retain an inlined vanilla height-200 boundary.
		int underworldCeiling = Math.Min(Main.maxTilesY - 520,
			Main.maxTilesY - Worldgen.NativeUnderworldSystem.ScaleOffset(200) - 320);
		int deepStart = cavernMin + Math.Max(80, (underworldCeiling - cavernMin) * 55 / 100);
		foreach (ResidenceAltitudeBand band in contract.AllowedLayers)
		{
			int min;
			int max;
			switch (band)
			{
				case ResidenceAltitudeBand.Underground:
					min = undergroundMin;
					max = Math.Max(min, cavernMin - height / 2 - 24);
					break;
				case ResidenceAltitudeBand.Cavern:
					min = cavernMin;
					max = deepStart - height;
					break;
				case ResidenceAltitudeBand.DeepCavern:
					min = deepStart;
					max = underworldCeiling - height;
					break;
				default:
					continue;
			}

			max = Math.Min(max, underworldCeiling - height);
			if (min <= max)
				ranges.Add((band, min, max));
		}

		return ranges;
	}
}
