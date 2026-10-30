using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;
/// <summary>
/// Technical deployment contracts for the 61 distinct candidate assignments.
/// This is deliberately separate from the approved runtime manifest: none of
/// these profiles authorizes worldgen before player visual acceptance.
/// </summary>
public static partial class AuthoredCandidateDeploymentManifest
{
	private static HomeBiome ResolveTargetBiome(string npcKey, HomeBiome homeBiome,
		AuthoredPlacementMode sourceMode)
	{
		// 30.10: the compact cavern houses keep the forest-column target they were tuned for,
		// whoever lives in them (now the residents that love the Underground).
		if (npcKey is "ThoriumMod/Blacksmith" or "MagicStorage/Golem" or "Terraria/GoblinTinkerer") return HomeBiome.Forest;
		if (sourceMode != AuthoredPlacementMode.TerrainScene ||
			homeBiome != HomeBiome.Underground) return homeBiome;
		return npcKey == "Terraria/Clothier" ? HomeBiome.Desert : HomeBiome.Forest;
	}

	private static DeploymentTargetAnchor ResolveTargetAnchor(string npcKey, HomeBiome biome, AuthoredPlacementMode sourceMode)
	{
		// The authored asset format describes how the source scene is decoded; it
		// does not require every decoded terrain scene to be deployed on the same
		// worldSurface band. Only self-contained compact structures are reassigned
		// underground. TerrainScene assets preserve a boundary connected to natural
		// surface terrain and cannot keep housing closed in an open compiler cavern.
		// 30.10: the three compact cavern houses moved to the residents that love the Underground.
		if (npcKey is "ThoriumMod/Blacksmith" or "MagicStorage/Golem" or "Terraria/GoblinTinkerer")
		{
			return DeploymentTargetAnchor.OrganicCavernEnvelope;
		}

		if (biome == HomeBiome.Underground)
			return DeploymentTargetAnchor.OrganicCavernEnvelope;
		if (biome == HomeBiome.Mushroom)
			return DeploymentTargetAnchor.OrganicMushroomCavernEnvelope;
		if (biome == HomeBiome.Underworld)
			return DeploymentTargetAnchor.UnderworldAshLavaShelf;
		if (npcKey is "AlchemistNPCLite/Brewer" or "FishermanNPC/Fisherman" or "ThoriumMod/Diverman" or "UnuBattleRods/Fishylady" or "CalValEX/JellyPriestNPC")
		{
			// WestBeachCottage permits a forest grade, Fisherman's compact smokehouse asks
			// for an ocean harbor shelf, a lighthouse belongs on coastal ground, and the
			// Mermaid Grotto House and Jelly Priest's compact harbor house are shore
			// residences rather than additional galleons on the exact waterline.
			// Treating these scenes as point-sized waterline homes leaves no disjoint exact
			// mutation masks beside SeaKing. Terrain envelopes keep them coastal while true
			// waterline homes retain water.
			return DeploymentTargetAnchor.SurfaceTerrainEnvelope;
		}

		return sourceMode switch
		{
			AuthoredPlacementMode.SurfaceStructure => DeploymentTargetAnchor.SurfaceGroundContact,
			AuthoredPlacementMode.TerrainScene => DeploymentTargetAnchor.SurfaceTerrainEnvelope,
			AuthoredPlacementMode.HarborScene => DeploymentTargetAnchor.CoastalWaterlineEnvelope,
			AuthoredPlacementMode.FloatingScene => DeploymentTargetAnchor.SkyClearanceEnvelope,
			AuthoredPlacementMode.UndergroundPoiScene => DeploymentTargetAnchor.OrganicCavernEnvelope,
			AuthoredPlacementMode.UnderworldPoiScene => DeploymentTargetAnchor.UnderworldAshLavaShelf,
			_ => throw new ArgumentOutOfRangeException(nameof(sourceMode), sourceMode, null)};
	}

	private static (int ProtectedPadding, int BlendMargin) ResolveMargins(DeploymentTargetAnchor targetAnchor) => targetAnchor switch
	{
		DeploymentTargetAnchor.SurfaceGroundContact => (8, 0),
		DeploymentTargetAnchor.SurfaceTerrainEnvelope => (24, 16),
		DeploymentTargetAnchor.CoastalWaterlineEnvelope => (32, 24),
		DeploymentTargetAnchor.SkyClearanceEnvelope => (24, 16),
		DeploymentTargetAnchor.OrganicCavernEnvelope => (36, 28),
		DeploymentTargetAnchor.OrganicMushroomCavernEnvelope => (36, 28),
		DeploymentTargetAnchor.UnderworldAshLavaShelf => (36, 28),
		_ => throw new ArgumentOutOfRangeException(nameof(targetAnchor), targetAnchor, null)};
}
