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
	private static void AddUndergroundPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "MagicStorage/Golem", ResidenceKind.Landmark, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Underground, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-forest-surface-citadel", ResidenceClusterPolicy.Isolated, 5, 180, 1, 4);
		Add(entries, "miningcracks_take_on_luiafk/Deeps", ResidenceKind.CavernResidence, ResidenceScaleClass.Large, Biomes(HomeBiome.Underground), Layers(ResidenceAltitudeBand.Cavern, ResidenceAltitudeBand.DeepCavern), ResidenceAltitudeBand.Cavern, ResidenceAnchorMode.OrganicCavern, "organic-cavern-lake", ResidenceClusterPolicy.Isolated, 3, 120, 1, 3);
		Add(entries, "Terraria/Clothier", ResidenceKind.Manor, ResidenceScaleClass.Medium, Biomes(HomeBiome.Underground, HomeBiome.Desert), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "desert-surface-gothic-manor", ResidenceClusterPolicy.Isolated, 2, 80, 2, 2);
		Add(entries, "Terraria/Demolitionist", ResidenceKind.Workshop, ResidenceScaleClass.Large, Biomes(HomeBiome.Underground), Layers(ResidenceAltitudeBand.Cavern), ResidenceAltitudeBand.Cavern, ResidenceAnchorMode.OrganicCavern, "organic-cavern-lake", ResidenceClusterPolicy.Isolated, 3, 120, 1, 3);
		Add(entries, "Terraria/GoblinTinkerer", ResidenceKind.Workshop, ResidenceScaleClass.Large, Biomes(HomeBiome.Underground, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-surface-workshop", ResidenceClusterPolicy.Isolated, 3, 96, 1, 3);
		Add(entries, "ThoriumMod/Blacksmith", ResidenceKind.Castle, ResidenceScaleClass.Mega, Biomes(HomeBiome.Underground, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-forest-surface-castle", ResidenceClusterPolicy.Isolated, 8, 240, 1, 5);
	}

	private static void AddHallowCorePlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "CalValEX/OracleNPC", ResidenceKind.FloatingIsland, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Hallow), Layers(ResidenceAltitudeBand.MidSky, ResidenceAltitudeBand.HighSky), ResidenceAltitudeBand.MidSky, ResidenceAnchorMode.SkyClearance, "open-sky-over-purity-or-hallow", ResidenceClusterPolicy.Isolated, 5, 160, 1, 4);
		Add(entries, "Fargowiltas/Deviantt", ResidenceKind.Castle, ResidenceScaleClass.Mega, Biomes(HomeBiome.Hallow, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-purity-or-hallow-shelf", ResidenceClusterPolicy.Isolated, 8, 260, 1, 5, ResidenceMacroWindow.HallowEitherShelf);
		Add(entries, "Terraria/DD2Bartender", ResidenceKind.House, ResidenceScaleClass.Large, Biomes(HomeBiome.Hallow, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "purity-or-hallow-grade", ResidenceClusterPolicy.Isolated, 3, 96, 1, 3, ResidenceMacroWindow.HallowEitherShelf);
	}

	private static void AddSnowHallowClinicPlacement(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "Terraria/Nurse", ResidenceKind.House, ResidenceScaleClass.Large, Biomes(HomeBiome.Snow, HomeBiome.Hallow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "snow-or-purity-hallow-grade", ResidenceClusterPolicy.Isolated, 3, 96, 1, 3);
	}
}
