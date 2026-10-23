using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	private static void AddForestVanillaThoriumResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		Add(entries, ref variant, "Terraria/Golfer", "Forest Clubhouse", HomeBiome.Forest, HomeArchetype.Entertainer, 45, 2, TownRoofStyle.Gambrel, TownFeature.Porch | TownFeature.Balcony | TownFeature.Fountain, 29, "GolfTee", 25);
		Add(entries, ref variant, "Terraria/Guide", "Guide's Library Lodge", HomeBiome.Forest, HomeArchetype.Scholar, 39, 2, TownRoofStyle.Gable, TownFeature.Tower | TownFeature.Porch | TownFeature.Chimney, 10, "Book", 20);
		Add(entries, ref variant, "Terraria/Merchant", "Old Forest Trading Post", HomeBiome.Forest, HomeArchetype.Trader, 47, 2, TownRoofStyle.Mansard, TownFeature.Awning | TownFeature.Porch | TownFeature.Balcony, 16, "Safe", 100);
		Add(entries, ref variant, "ThoriumMod/Cobbler", "Cobbler Rowhouse", HomeBiome.Forest, HomeArchetype.Trader, 31, 3, TownRoofStyle.Mansard, TownFeature.Awning | TownFeature.Workshop | TownFeature.Balcony, 11, "Loom", 45);
		Add(entries, ref variant, "ThoriumMod/ConfusedZombie", "Reclaimed Ivy Ruin", HomeBiome.Forest, HomeArchetype.Scholar, 37, 2, TownRoofStyle.Vault, TownFeature.Garden | TownFeature.Shrine | TownFeature.Chimney, 21, "GraveMarker", 25);
		Add(entries, ref variant, "ThoriumMod/Cook", "Great Hearth Inn", HomeBiome.Forest, HomeArchetype.Chef, 49, 2, TownRoofStyle.Gambrel, TownFeature.Chimney | TownFeature.Awning | TownFeature.Porch, 14, "CookingPot", 50);
		Add(entries, ref variant, "ThoriumMod/Tracker", "Ranger Watch Lodge", HomeBiome.Forest, HomeArchetype.Warrior, 37, 3, TownRoofStyle.Spire, TownFeature.Tower | TownFeature.Tree | TownFeature.Balcony, 5, "Binoculars", 75);
		Add(entries, ref variant, "ThoriumMod/WeaponMaster", "Weapon Master's Dojo", HomeBiome.Forest, HomeArchetype.Warrior, 45, 2, TownRoofStyle.Pagoda, TownFeature.Porch | TownFeature.Shrine | TownFeature.Workshop, 23, "WeaponRack", 65);
	}

	private static void AddSnowResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Snow town.
		Add(entries, ref variant, "CalamityMod/Archmage", "Permafrost Observatory", HomeBiome.Snow, HomeArchetype.Mystic, 43, 3, TownRoofStyle.Spire, TownFeature.Observatory | TownFeature.Tower | TownFeature.Shrine, 30, "CrystalBall", 100);
		Add(entries, ref variant, "Terraria/Cyborg", "Cryogenic Laboratory", HomeBiome.Snow, HomeArchetype.Engineer, 47, 2, TownRoofStyle.Dome, TownFeature.Observatory | TownFeature.Workshop | TownFeature.Tower, 9, "Nanites", 80);
		Add(entries, ref variant, "Terraria/Mechanic", "Boreal Gearworks", HomeBiome.Snow, HomeArchetype.Engineer, 41, 2, TownRoofStyle.Sawtooth, TownFeature.Workshop | TownFeature.Chimney | TownFeature.Balcony, 20, "Wrench", 40);
		Add(entries, ref variant, "Terraria/SantaClaus", "Northwind Toy Workshop", HomeBiome.Snow, HomeArchetype.Entertainer, 49, 2, TownRoofStyle.Gambrel, TownFeature.Chimney | TownFeature.Porch | TownFeature.Workshop, 4, "ChristmasTree", 60);
		Add(entries, ref variant, "Terraria/TaxCollector", "Frost Ledger Hall", HomeBiome.Snow, HomeArchetype.Trader, 37, 3, TownRoofStyle.Mansard, TownFeature.Tower | TownFeature.Balcony | TownFeature.Chimney, 13, "PiggyBank", 50);
	}
}
