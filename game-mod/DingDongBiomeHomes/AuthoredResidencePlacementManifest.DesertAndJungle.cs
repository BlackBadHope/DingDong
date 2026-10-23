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
	private static void AddDesertPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "CalamityMod/Bandit", ResidenceKind.House, ResidenceScaleClass.Large, Biomes(HomeBiome.Desert), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "desert-shelf", ResidenceClusterPolicy.TownEdge, 3, 80, 2, 2);
		Add(entries, "Terraria/ArmsDealer", ResidenceKind.Manor, ResidenceScaleClass.Large, Biomes(HomeBiome.Desert), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "desert-surface-armory", ResidenceClusterPolicy.TownEdge, 3, 80, 2, 2);
		Add(entries, "Terraria/DyeTrader", ResidenceKind.Landmark, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Desert), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-desert-surface-palace", ResidenceClusterPolicy.Isolated, 5, 180, 1, 4);
		Add(entries, "Terraria/Steampunker", ResidenceKind.Manor, ResidenceScaleClass.Medium, Biomes(HomeBiome.Desert), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "desert-surface-engine-room", ResidenceClusterPolicy.TownEdge, 2, 56, 3, 2);
		Add(entries, "ThoriumMod/DesertAcolyte", ResidenceKind.FloatingIsland, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Desert), Layers(ResidenceAltitudeBand.HighSky, ResidenceAltitudeBand.MidSky), ResidenceAltitudeBand.MidSky, ResidenceAnchorMode.SkyClearance, "open-sky-over-desert", ResidenceClusterPolicy.Isolated, 5, 180, 1, 4);
	}

	private static void AddJunglePlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "CalValEX/Jharim", ResidenceKind.Treehouse, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Jungle), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "jungle-canopy", ResidenceClusterPolicy.Isolated, 5, 160, 1, 4);
		Add(entries, "Terraria/Dryad", ResidenceKind.Manor, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Jungle), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "jungle-canopy", ResidenceClusterPolicy.TownEdge, 5, 96, 2, 3);
		Add(entries, "Terraria/Painter", ResidenceKind.House, ResidenceScaleClass.Medium, Biomes(HomeBiome.Jungle), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "jungle-grade", ResidenceClusterPolicy.TownEdge, 2, 56, 3, 2);
		Add(entries, "Terraria/WitchDoctor", ResidenceKind.Shrine, ResidenceScaleClass.Large, Biomes(HomeBiome.Jungle), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "jungle-surface-shrine", ResidenceClusterPolicy.TownEdge, 3, 80, 2, 2);
		Add(entries, "ThoriumMod/Druid", ResidenceKind.Treehouse, ResidenceScaleClass.Large, Biomes(HomeBiome.Jungle), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "jungle-surface-grove", ResidenceClusterPolicy.TownEdge, 3, 80, 2, 2);
	}
}
