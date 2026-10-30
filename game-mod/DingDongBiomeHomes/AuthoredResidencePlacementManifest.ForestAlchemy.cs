using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
/// <summary>
/// Spatial deployment metadata for the 61 authored residences.
/// This is deliberately separate from <see cref = "NpcTownManifest"/> lore and
/// from the approved visual runtime manifest. NPC identity biome remains an
/// allowed lore constraint, while the first allowed biome is the deployment
/// preference chosen for the authored asset.
/// </summary>
public static partial class AuthoredResidencePlacementManifest
{
	private static void AddForestAlchemyPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "AlchemistNPCLite/Alchemist", ResidenceKind.Laboratory, ResidenceScaleClass.Compact, Biomes(HomeBiome.Forest, HomeBiome.Jungle), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.SurfaceGround, "forest-or-jungle-garden-soil", ResidenceClusterPolicy.Hamlet, 1, 24, 4, 1);
		Add(entries, "AlchemistNPCLite/Architect", ResidenceKind.House, ResidenceScaleClass.Large, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-surface-street", ResidenceClusterPolicy.TownEdge, 3, 80, 2, 2);
		Add(entries, "AlchemistNPCLite/Brewer", ResidenceKind.Cottage, ResidenceScaleClass.Medium, Biomes(HomeBiome.Forest, HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.CoastalWaterline, "beach-or-forest-grade", ResidenceClusterPolicy.Port, 2, 48, 3, 2);
		Add(entries, "AlchemistNPCLite/Jeweler", ResidenceKind.Castle, ResidenceScaleClass.Mega, Biomes(HomeBiome.Underground, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-forest-surface-castle", ResidenceClusterPolicy.Isolated, 8, 240, 1, 5);
		Add(entries, "AlchemistNPCLite/Musician", ResidenceKind.Manor, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "broad-forest-shelf", ResidenceClusterPolicy.Isolated, 5, 160, 1, 4);
		Add(entries, "AlchemistNPCLite/Operator", ResidenceKind.Landmark, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Underground, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-forest-surface-citadel", ResidenceClusterPolicy.Isolated, 5, 180, 1, 4);
		Add(entries, "AlchemistNPCLite/Tinkerer", ResidenceKind.Workshop, ResidenceScaleClass.Large, Biomes(HomeBiome.Underground, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-surface-workshop", ResidenceClusterPolicy.Isolated, 3, 96, 1, 3);
		Add(entries, "AlchemistNPCLite/YoungBrewer", ResidenceKind.Landmark, ResidenceScaleClass.Mega, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-forest-shelf", ResidenceClusterPolicy.Isolated, 8, 240, 1, 5);
	}
}
