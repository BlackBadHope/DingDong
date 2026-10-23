using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	private static void AddOceanResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Ocean port.
		Add(entries, ref variant, "CalamityMod/SeaKing", "Tide King's Palace", HomeBiome.Ocean, HomeArchetype.Royal, 49, 3, TownRoofStyle.Dome, TownFeature.Dock | TownFeature.Tower | TownFeature.Fountain, 30, "Coral", 60);
		Add(entries, ref variant, "CalValEX/JellyPriestNPC", "Jellyfish Coral Chapel", HomeBiome.Ocean, HomeArchetype.Mystic, 39, 3, TownRoofStyle.Dome, TownFeature.Shrine | TownFeature.Dock | TownFeature.Observatory, 25, "JellyfishJar", 55);
		Add(entries, ref variant, "FishermanNPC/Fisherman", "Old Fisherman's Smokehouse", HomeBiome.Ocean, HomeArchetype.Angler, 37, 2, TownRoofStyle.Gambrel, TownFeature.Dock | TownFeature.Chimney | TownFeature.Porch, 10, "FishBowl", 25);
		Add(entries, ref variant, "Terraria/Angler", "Bright Bait Shack", HomeBiome.Ocean, HomeArchetype.Angler, 33, 2, TownRoofStyle.Hull, TownFeature.Dock | TownFeature.Awning | TownFeature.Balcony, 19, "ReinforcedFishingPole", 35);
		Add(entries, ref variant, "Terraria/Pirate", "Corsair Galleon House", HomeBiome.Ocean, HomeArchetype.Angler, 49, 3, TownRoofStyle.Hull, TownFeature.Dock | TownFeature.Tower | TownFeature.Balcony, 13, "Cannon", 100);
		Add(entries, ref variant, "Terraria/Stylist", "Seabreeze Salon", HomeBiome.Ocean, HomeArchetype.Artist, 41, 2, TownRoofStyle.Mansard, TownFeature.Awning | TownFeature.Dock | TownFeature.Balcony, 27, "Dresser", 55);
		Add(entries, ref variant, "ThoriumMod/Diverman", "Diving Bell Laboratory", HomeBiome.Ocean, HomeArchetype.Angler, 43, 2, TownRoofStyle.Dome, TownFeature.Dock | TownFeature.Observatory | TownFeature.Workshop, 8, "DivingHelmet", 80);
		Add(entries, ref variant, "UnuBattleRods/Fishylady", "Mermaid Grotto House", HomeBiome.Ocean, HomeArchetype.Angler, 45, 2, TownRoofStyle.Vault, TownFeature.Dock | TownFeature.Fountain | TownFeature.Shrine, 22, "Starfish", 45);
	}

	private static void AddUndergroundResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Cavern industrial town.
		Add(entries, ref variant, "MagicStorage/Golem", "Arcane Storage Vault", HomeBiome.Underground, HomeArchetype.Engineer, 47, 3, TownRoofStyle.Vault, TownFeature.Workshop | TownFeature.Shrine | TownFeature.Tower, 25, "Safe", 100);
		Add(entries, ref variant, "miningcracks_take_on_luiafk/Deeps", "Deep Mine Station", HomeBiome.Underground, HomeArchetype.Engineer, 45, 2, TownRoofStyle.Flat, TownFeature.Workshop | TownFeature.Forge | TownFeature.Tower, 8, "MinecartTrack", 35);
		Add(entries, ref variant, "Terraria/Clothier", "Catacomb Tailor Hall", HomeBiome.Underground, HomeArchetype.Artist, 39, 2, TownRoofStyle.Vault, TownFeature.Crypt | TownFeature.Workshop | TownFeature.Awning, 11, "Loom", 45);
		Add(entries, ref variant, "Terraria/Demolitionist", "Cavern Blastworks", HomeBiome.Underground, HomeArchetype.Engineer, 43, 2, TownRoofStyle.Sawtooth, TownFeature.Workshop | TownFeature.Forge | TownFeature.Chimney, 16, "Explosives", 50);
		Add(entries, ref variant, "Terraria/GoblinTinkerer", "Goblin Mechanist Burrow", HomeBiome.Underground, HomeArchetype.Engineer, 37, 2, TownRoofStyle.Vault, TownFeature.Workshop | TownFeature.Awning | TownFeature.Tower, 20, "TinkerersWorkshop", 100);
		Add(entries, ref variant, "ThoriumMod/Blacksmith", "Basalt Grand Forge", HomeBiome.Underground, HomeArchetype.Engineer, 49, 3, TownRoofStyle.Sawtooth, TownFeature.Forge | TownFeature.Chimney | TownFeature.Workshop, 1, "HeavyWorkBench", 75);
	}
}
