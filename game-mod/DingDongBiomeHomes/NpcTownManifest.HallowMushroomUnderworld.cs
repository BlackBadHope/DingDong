using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
public static partial class NpcTownManifest
{
	private static void AddHallowResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Hallow city. This district is intentionally synthesized on pre-hardmode FTW worlds.
		Add(entries, ref variant, "CalValEX/OracleNPC", "Crystal Oracle Rotunda", HomeBiome.Hallow, HomeArchetype.Mystic, 43, 3, TownRoofStyle.Dome, TownFeature.Observatory | TownFeature.Shrine | TownFeature.Fountain, 25, "CrystalShard", 75);
		Add(entries, ref variant, "Fargowiltas/Deviantt", "Prismatic Curio Boutique", HomeBiome.Hallow, HomeArchetype.Trader, 39, 2, TownRoofStyle.Mansard, TownFeature.Awning | TownFeature.Balcony | TownFeature.Fountain, 27, "CrystalBall", 80);
		Add(entries, ref variant, "Terraria/DD2Bartender", "Aether Defender Tavern", HomeBiome.Hallow, HomeArchetype.Warrior, 47, 2, TownRoofStyle.Gambrel, TownFeature.Porch | TownFeature.Chimney | TownFeature.Shrine, 18, "WarTable", 65);
		Add(entries, ref variant, "Terraria/Nurse", "Hallowed Sanctuary Clinic", HomeBiome.Hallow, HomeArchetype.Healer, 45, 2, TownRoofStyle.Dome, TownFeature.Greenhouse | TownFeature.Fountain | TownFeature.Porch, 29, "RestorationPotion", 50);
		Add(entries, ref variant, "Terraria/PartyGirl", "Confetti Pavilion", HomeBiome.Hallow, HomeArchetype.Entertainer, 41, 2, TownRoofStyle.Pagoda, TownFeature.Balcony | TownFeature.Fountain | TownFeature.Awning, 19, "BubbleMachine", 60);
		Add(entries, ref variant, "Terraria/Princess", "Rose Crystal Palace", HomeBiome.Hallow, HomeArchetype.Royal, 49, 3, TownRoofStyle.Spire, TownFeature.Tower | TownFeature.Balcony | TownFeature.Fountain, 30, "Throne", 100);
		Add(entries, ref variant, "Terraria/Wizard", "Arcane Star Tower", HomeBiome.Hallow, HomeArchetype.Mystic, 35, 3, TownRoofStyle.Spire, TownFeature.Tower | TownFeature.Observatory | TownFeature.Shrine, 25, "SpellTome", 75);
		Add(entries, ref variant, "ThoriumMod/Spiritualist", "Spirit Lantern Chapel", HomeBiome.Hallow, HomeArchetype.Mystic, 37, 2, TownRoofStyle.Pagoda, TownFeature.Shrine | TownFeature.Garden | TownFeature.Tower, 22, "BewitchingTable", 75);
		Add(entries, ref variant, "WeaponEnchantments/Witch", "Enchanter Coven House", HomeBiome.Hallow, HomeArchetype.Mystic, 43, 3, TownRoofStyle.Mansard, TownFeature.Shrine | TownFeature.Observatory | TownFeature.Greenhouse, 28, "CrystalBall", 90);
	}

	private static void AddMushroomResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		// Mushroom enclave and Underworld citadel.
		Add(entries, ref variant, "Terraria/Truffle", "Luminous Spore Dome", HomeBiome.Mushroom, HomeArchetype.Naturalist, 49, 2, TownRoofStyle.Dome, TownFeature.Greenhouse | TownFeature.Garden | TownFeature.Fountain, 21, "MushroomWorkbench", 65);
	}

	private static void AddUnderworldResidents(Dictionary<string, NpcTownIdentity> entries, ref int variant)
	{
		Add(entries, ref variant, "CalamityMod/BrimstoneWitch", "Brimstone Witch Spire", HomeBiome.Underworld, HomeArchetype.Mystic, 43, 3, TownRoofStyle.Spire, TownFeature.Tower | TownFeature.Shrine | TownFeature.Forge, 1, "Obsidian", 55);
		Add(entries, ref variant, "Fargowiltas/Abominationn", "Ashen Abomination Monastery", HomeBiome.Underworld, HomeArchetype.Warrior, 49, 3, TownRoofStyle.Pagoda, TownFeature.Forge | TownFeature.Shrine | TownFeature.Chimney, 14, "Hellforge", 100);
	}
}
