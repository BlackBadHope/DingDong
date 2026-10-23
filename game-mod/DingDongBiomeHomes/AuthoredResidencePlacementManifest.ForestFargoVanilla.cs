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
	private static void AddForestFargoVanillaPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "Fargowiltas/LumberJack", ResidenceKind.Castle, ResidenceScaleClass.Mega, Biomes(HomeBiome.Forest, HomeBiome.Snow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-forest-or-snow-shelf", ResidenceClusterPolicy.Isolated, 8, 280, 1, 5);
		Add(entries, "Fargowiltas/Mutant", ResidenceKind.FloatingIsland, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.HighSky, ResidenceAltitudeBand.MidSky), ResidenceAltitudeBand.HighSky, ResidenceAnchorMode.SkyClearance, "open-sky-over-forest", ResidenceClusterPolicy.Isolated, 5, 180, 1, 4);
		Add(entries, "Fargowiltas/Squirrel", ResidenceKind.Treehouse, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Forest, HomeBiome.Jungle), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-or-jungle-canopy", ResidenceClusterPolicy.Isolated, 5, 140, 1, 3);
		Add(entries, "Terraria/BestiaryGirl", ResidenceKind.Airship, ResidenceScaleClass.Large, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.MidSky, ResidenceAltitudeBand.LowSky), ResidenceAltitudeBand.MidSky, ResidenceAnchorMode.SkyClearance, "open-sky-over-forest", ResidenceClusterPolicy.Isolated, 3, 120, 1, 3);
		Add(entries, "Terraria/Golfer", ResidenceKind.House, ResidenceScaleClass.Medium, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "hill-or-forest-grade", ResidenceClusterPolicy.Isolated, 2, 64, 2, 2);
		Add(entries, "Terraria/Guide", ResidenceKind.Manor, ResidenceScaleClass.Large, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-hill-grade", ResidenceClusterPolicy.Isolated, 3, 96, 1, 3);
		Add(entries, "Terraria/Merchant", ResidenceKind.House, ResidenceScaleClass.Compact, Biomes(HomeBiome.Forest, HomeBiome.Desert), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.SurfaceGround, "forest-or-desert-trading-grade", ResidenceClusterPolicy.Hamlet, 1, 24, 4, 1);
		Add(entries, "ThoriumMod/Cobbler", ResidenceKind.House, ResidenceScaleClass.Compact, Biomes(HomeBiome.Forest, HomeBiome.Snow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.SurfaceGround, "forest-or-snow-street-grade", ResidenceClusterPolicy.Hamlet, 1, 24, 4, 1);
		Add(entries, "ThoriumMod/ConfusedZombie", ResidenceKind.Tower, ResidenceScaleClass.Large, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-canopy", ResidenceClusterPolicy.Isolated, 3, 96, 1, 3);
	}
}
