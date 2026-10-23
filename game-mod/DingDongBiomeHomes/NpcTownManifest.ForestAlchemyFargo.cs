using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	private static void AddForestAlchemyFargoResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Forest city: workshops, guild houses and lived-in civic buildings.
		Add(entries, ref variant, "AlchemistNPCLite/Alchemist", "Glasshouse Apothecary", HomeBiome.Forest, HomeArchetype.Healer, 41, 2, TownRoofStyle.Gable, TownFeature.Greenhouse | TownFeature.Garden | TownFeature.Chimney, 26, "AlchemyTable", 75);
		Add(entries, ref variant, "AlchemistNPCLite/Architect", "Drafting Guildhall", HomeBiome.Forest, HomeArchetype.Engineer, 47, 3, TownRoofStyle.Mansard, TownFeature.Tower | TownFeature.Balcony | TownFeature.Workshop, 12, "Toolbox", 60);
		Add(entries, ref variant, "AlchemistNPCLite/Brewer", "Copper Kettle Brewery", HomeBiome.Forest, HomeArchetype.Healer, 43, 2, TownRoofStyle.Gambrel, TownFeature.Chimney | TownFeature.Porch | TownFeature.Workshop, 18, "Keg", 45);
		Add(entries, ref variant, "AlchemistNPCLite/Jeweler", "Gemcutter Atelier", HomeBiome.Forest, HomeArchetype.Trader, 37, 2, TownRoofStyle.Dome, TownFeature.Awning | TownFeature.Fountain | TownFeature.Workshop, 27, "GemLockRuby", 80);
		Add(entries, ref variant, "AlchemistNPCLite/Musician", "Woodland Concert Hall", HomeBiome.Forest, HomeArchetype.Entertainer, 49, 2, TownRoofStyle.Gable, TownFeature.Balcony | TownFeature.Porch | TownFeature.Tower, 19, "Harp", 100);
		Add(entries, ref variant, "AlchemistNPCLite/Operator", "Telegraph Exchange", HomeBiome.Forest, HomeArchetype.Engineer, 39, 3, TownRoofStyle.Flat, TownFeature.Tower | TownFeature.Observatory | TownFeature.Workshop, 8, "Wire", 30);
		Add(entries, ref variant, "AlchemistNPCLite/Tinkerer", "Clockwork Shed", HomeBiome.Forest, HomeArchetype.Engineer, 35, 2, TownRoofStyle.Sawtooth, TownFeature.Workshop | TownFeature.Chimney | TownFeature.Awning, 17, "Actuator", 40);
		Add(entries, ref variant, "AlchemistNPCLite/YoungBrewer", "Young Brewer Herb Cottage", HomeBiome.Forest, HomeArchetype.Healer, 33, 1, TownRoofStyle.Gable, TownFeature.Garden | TownFeature.Greenhouse | TownFeature.Porch, 3, "ClayPot", 20);
		Add(entries, ref variant, "Fargowiltas/LumberJack", "Timber Mill Lodge", HomeBiome.Forest, HomeArchetype.Engineer, 45, 2, TownRoofStyle.Gambrel, TownFeature.Workshop | TownFeature.Tree | TownFeature.Chimney, 7, "Sawmill", 50);
		Add(entries, ref variant, "Fargowiltas/Mutant", "Mutagen Observatory", HomeBiome.Forest, HomeArchetype.Warrior, 41, 3, TownRoofStyle.Spire, TownFeature.Observatory | TownFeature.Tower | TownFeature.Fountain, 25, "SharpeningStation", 90);
		Add(entries, ref variant, "Fargowiltas/Squirrel", "Acorn Canopy Lodge", HomeBiome.Forest, HomeArchetype.Naturalist, 35, 2, TownRoofStyle.Canopy, TownFeature.Tree | TownFeature.Balcony | TownFeature.Garden, 6, "Acorn", 15);
		Add(entries, ref variant, "Terraria/BestiaryGirl", "Wildlife Sanctuary", HomeBiome.Forest, HomeArchetype.Naturalist, 43, 2, TownRoofStyle.Canopy, TownFeature.Tree | TownFeature.Greenhouse | TownFeature.Garden, 28, "Terrarium", 55);
	}
}
