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
	private static void AddForestThoriumPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "ThoriumMod/Cook", ResidenceKind.House, ResidenceScaleClass.Compact, Biomes(HomeBiome.Forest, HomeBiome.Desert), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.SurfaceGround, "forest-or-desert-surface-grade", ResidenceClusterPolicy.Hamlet, 1, 24, 4, 1);
		Add(entries, "ThoriumMod/Tracker", ResidenceKind.Treehouse, ResidenceScaleClass.Medium, Biomes(HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-canopy", ResidenceClusterPolicy.Isolated, 2, 72, 2, 2);
		Add(entries, "ThoriumMod/WeaponMaster", ResidenceKind.Castle, ResidenceScaleClass.Medium, Biomes(HomeBiome.Forest, HomeBiome.Hallow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.SurfaceGround, "forest-or-hallow-stone-grade", ResidenceClusterPolicy.TownEdge, 2, 56, 3, 2);
	}

	private static void AddSnowCorePlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "CalamityMod/Archmage", ResidenceKind.Castle, ResidenceScaleClass.Mega, Biomes(HomeBiome.Snow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-snow-shelf", ResidenceClusterPolicy.Isolated, 8, 240, 1, 5);
		Add(entries, "Terraria/Cyborg", ResidenceKind.Airship, ResidenceScaleClass.Large, Biomes(HomeBiome.Snow), Layers(ResidenceAltitudeBand.MidSky, ResidenceAltitudeBand.HighSky), ResidenceAltitudeBand.MidSky, ResidenceAnchorMode.SkyClearance, "open-sky-over-snow", ResidenceClusterPolicy.Isolated, 3, 120, 1, 3);
		Add(entries, "Terraria/Mechanic", ResidenceKind.Manor, ResidenceScaleClass.Large, Biomes(HomeBiome.Snow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "snow-surface-workshop", ResidenceClusterPolicy.TownEdge, 3, 80, 2, 2);
		Add(entries, "Terraria/SantaClaus", ResidenceKind.Castle, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Snow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "snow-shelf", ResidenceClusterPolicy.TownEdge, 5, 96, 2, 3);
		Add(entries, "Terraria/TaxCollector", ResidenceKind.Manor, ResidenceScaleClass.Large, Biomes(HomeBiome.Snow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "snow-surface-vault", ResidenceClusterPolicy.TownEdge, 3, 80, 2, 2);
	}
}
