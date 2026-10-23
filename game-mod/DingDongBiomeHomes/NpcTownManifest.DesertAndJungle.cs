using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	private static void AddDesertResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Desert city.
		Add(entries, ref variant, "CalamityMod/Bandit", "Smuggler Casbah", HomeBiome.Desert, HomeArchetype.Trader, 41, 2, TownRoofStyle.Dome, TownFeature.Awning | TownFeature.Balcony | TownFeature.Tower, 18, "GoldDust", 70);
		Add(entries, ref variant, "Terraria/ArmsDealer", "Dune Gunsmith Fort", HomeBiome.Desert, HomeArchetype.Warrior, 45, 2, TownRoofStyle.Flat, TownFeature.Tower | TownFeature.Workshop | TownFeature.Awning, 7, "AmmoBox", 65);
		Add(entries, ref variant, "Terraria/DyeTrader", "Prismatic Dye Souk", HomeBiome.Desert, HomeArchetype.Artist, 43, 2, TownRoofStyle.Dome, TownFeature.Awning | TownFeature.Fountain | TownFeature.Balcony, 27, "DyeVat", 55);
		Add(entries, ref variant, "Terraria/Steampunker", "Brass Sand Foundry", HomeBiome.Desert, HomeArchetype.Engineer, 49, 3, TownRoofStyle.Sawtooth, TownFeature.Chimney | TownFeature.Workshop | TownFeature.Tower, 17, "SteampunkBoiler", 100);
		Add(entries, ref variant, "ThoriumMod/DesertAcolyte", "Sun Acolyte Temple", HomeBiome.Desert, HomeArchetype.Mystic, 39, 3, TownRoofStyle.Pagoda, TownFeature.Shrine | TownFeature.Tower | TownFeature.Fountain, 12, "SunplateBlock", 50);
	}

	private static void AddJungleResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Jungle village.
		Add(entries, ref variant, "CalValEX/Jharim", "Jharim Jungle Shrine", HomeBiome.Jungle, HomeArchetype.Royal, 47, 3, TownRoofStyle.Pagoda, TownFeature.Shrine | TownFeature.Tower | TownFeature.Tree, 3, "DynastyWood", 75);
		Add(entries, ref variant, "Terraria/Dryad", "Living Grove House", HomeBiome.Jungle, HomeArchetype.Naturalist, 43, 2, TownRoofStyle.Canopy, TownFeature.Tree | TownFeature.Garden | TownFeature.Fountain, 6, "PlanterBox", 40);
		Add(entries, ref variant, "Terraria/Painter", "Canopy Artist Studio", HomeBiome.Jungle, HomeArchetype.Artist, 39, 2, TownRoofStyle.Gable, TownFeature.Balcony | TownFeature.Tree | TownFeature.Greenhouse, 26, "PaintSprayer", 55);
		Add(entries, ref variant, "Terraria/WitchDoctor", "Mahogany Spirit House", HomeBiome.Jungle, HomeArchetype.Mystic, 37, 3, TownRoofStyle.Pagoda, TownFeature.Shrine | TownFeature.Porch | TownFeature.Tree, 25, "ImbuingStation", 80);
		Add(entries, ref variant, "ThoriumMod/Druid", "Ancient Druid Conservatory", HomeBiome.Jungle, HomeArchetype.Naturalist, 45, 2, TownRoofStyle.Dome, TownFeature.Greenhouse | TownFeature.Garden | TownFeature.Shrine, 28, "LivingLoom", 75);
	}
}
