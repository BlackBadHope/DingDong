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
	private static void AddOceanPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "CalamityMod/SeaKing", ResidenceKind.Harbor, ResidenceScaleClass.Large, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.CoastalWaterline, "ocean-waterline", ResidenceClusterPolicy.Port, 3, 80, 4, 3);
		Add(entries, "CalValEX/JellyPriestNPC", ResidenceKind.Harbor, ResidenceScaleClass.Compact, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.CoastalWaterline, "ocean-waterline", ResidenceClusterPolicy.Port, 1, 32, 4, 1);
		Add(entries, "FishermanNPC/Fisherman", ResidenceKind.Harbor, ResidenceScaleClass.Large, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.CoastalWaterline, "ocean-harbor-shelf", ResidenceClusterPolicy.Port, 3, 80, 4, 3);
		Add(entries, "Terraria/Angler", ResidenceKind.Harbor, ResidenceScaleClass.Compact, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.CoastalWaterline, "ocean-waterline", ResidenceClusterPolicy.Port, 1, 32, 4, 1);
		Add(entries, "Terraria/Pirate", ResidenceKind.FloatingIsland, ResidenceScaleClass.Mega, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.HighSky, ResidenceAltitudeBand.MidSky), ResidenceAltitudeBand.HighSky, ResidenceAnchorMode.SkyClearance, "open-coastal-sky", ResidenceClusterPolicy.Isolated, 8, 260, 1, 5);
		Add(entries, "Terraria/Stylist", ResidenceKind.House, ResidenceScaleClass.Medium, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "coastal-grade", ResidenceClusterPolicy.Port, 2, 64, 3, 2);
		Add(entries, "ThoriumMod/Diverman", ResidenceKind.Tower, ResidenceScaleClass.Medium, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.CoastalWaterline, "ocean-waterline", ResidenceClusterPolicy.Port, 2, 56, 4, 1);
		Add(entries, "UnuBattleRods/Fishylady", ResidenceKind.Harbor, ResidenceScaleClass.Compact, Biomes(HomeBiome.Ocean), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.CoastalWaterline, "ocean-waterline", ResidenceClusterPolicy.Port, 1, 32, 4, 1);
	}
}
