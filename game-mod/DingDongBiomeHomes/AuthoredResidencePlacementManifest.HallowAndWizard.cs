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
	private static void AddHallowRoyalPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "Terraria/PartyGirl", ResidenceKind.Manor, ResidenceScaleClass.Mega, Biomes(HomeBiome.Hallow, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "isolated-purity-or-hallow-shelf", ResidenceClusterPolicy.Isolated, 8, 280, 1, 5, ResidenceMacroWindow.HallowEitherShelf);
		Add(entries, "Terraria/Princess", ResidenceKind.Manor, ResidenceScaleClass.Large, Biomes(HomeBiome.Hallow, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "hallow-cliff-or-purity-shelf", ResidenceClusterPolicy.Isolated, 3, 96, 1, 3, ResidenceMacroWindow.HallowEitherShelf);
	}

	private static void AddForestWizardPlacement(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "Terraria/Wizard", ResidenceKind.Tower, ResidenceScaleClass.Medium, Biomes(HomeBiome.Forest, HomeBiome.Hallow), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "forest-or-purity-hallow-grade", ResidenceClusterPolicy.Isolated, 2, 72, 2, 2);
	}

	private static void AddHallowModdedPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "ThoriumMod/Spiritualist", ResidenceKind.Shrine, ResidenceScaleClass.Large, Biomes(HomeBiome.Hallow), Layers(ResidenceAltitudeBand.LowSky, ResidenceAltitudeBand.MidSky), ResidenceAltitudeBand.LowSky, ResidenceAnchorMode.SkyClearance, "open-sky-over-purity-or-hallow", ResidenceClusterPolicy.Isolated, 3, 120, 1, 3);
		Add(entries, "WeaponEnchantments/Witch", ResidenceKind.Landmark, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Hallow, HomeBiome.Forest), Layers(ResidenceAltitudeBand.Surface), ResidenceAltitudeBand.Surface, ResidenceAnchorMode.TerrainEnvelope, "hallow-ridge-or-purity-shelf", ResidenceClusterPolicy.Isolated, 5, 160, 1, 4, ResidenceMacroWindow.HallowEitherShelf);
	}

	private static void AddMushroomPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "Terraria/Truffle", ResidenceKind.CavernResidence, ResidenceScaleClass.Large, Biomes(HomeBiome.Mushroom), Layers(ResidenceAltitudeBand.Cavern), ResidenceAltitudeBand.Cavern, ResidenceAnchorMode.OrganicCavern, "glowing-mushroom-cavern", ResidenceClusterPolicy.Isolated, 3, 120, 1, 3);
	}

	private static void AddUnderworldPlacements(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		Add(entries, "CalamityMod/BrimstoneWitch", ResidenceKind.UnderworldResidence, ResidenceScaleClass.Large, Biomes(HomeBiome.Underworld), Layers(ResidenceAltitudeBand.Underworld), ResidenceAltitudeBand.Underworld, ResidenceAnchorMode.UnderworldShelf, "underworld-ash-lava-shelf", ResidenceClusterPolicy.Isolated, 3, 120, 1, 3);
		Add(entries, "Fargowiltas/Abominationn", ResidenceKind.UnderworldResidence, ResidenceScaleClass.Landmark, Biomes(HomeBiome.Underworld), Layers(ResidenceAltitudeBand.Underworld), ResidenceAltitudeBand.Underworld, ResidenceAnchorMode.UnderworldShelf, "underworld-ash-lava-shelf", ResidenceClusterPolicy.Isolated, 5, 180, 1, 4);
	}
}
