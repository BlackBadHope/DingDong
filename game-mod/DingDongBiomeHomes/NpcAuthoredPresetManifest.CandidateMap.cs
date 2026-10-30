using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Explicit NPC-to-geometry assignments. This manifest is intentionally
/// fail-closed: world generation stays disabled until all 61 supported NPCs
/// have distinct reviewed authored presets.
/// </summary>
public static partial class NpcAuthoredPresetManifest
{
	private static Dictionary<string, string> BuildCandidates()
	{
		Dictionary<string, string> entries = new(StringComparer.Ordinal)
		{
			["AlchemistNPCLite/Alchemist"] = "Greenhouse1",
			["AlchemistNPCLite/Architect"] = "Greaveil_SpawnForestStreet",
			["AlchemistNPCLite/Brewer"] = "Greaveil_WestBeachCottage",
			["AlchemistNPCLite/Jeweler"] = "R7_UnrealDeltaCastle",
			["AlchemistNPCLite/Musician"] = "R7_NadikiMansion",
			["AlchemistNPCLite/Operator"] = "Aegis8Showcase_MoonCitadel",
			["AlchemistNPCLite/Tinkerer"] = "Ziiio_SteampunkerWorkshop",
			["AlchemistNPCLite/YoungBrewer"] = "R7_EsonoraHome",
			["Fargowiltas/LumberJack"] = "R7_CastleCoolResidence",
			["Fargowiltas/Mutant"] = "Aegis8_SteampunkOrb",
			["Fargowiltas/Squirrel"] = "Aegis8_ForestTreehouse",
			["Terraria/BestiaryGirl"] = "Huadiao_ElementalSkyHouse",
			["Terraria/Golfer"] = "Aegis8_MountainLodge",
			["Terraria/Guide"] = "Greaveil_HillManor",
			["Terraria/Merchant"] = "House1_Expination",
			["ThoriumMod/Cobbler"] = "House2_Expination",
			["ThoriumMod/ConfusedZombie"] = "Ziiio_JungleWatchtower",
			["ThoriumMod/Cook"] = "LargeHouse1",
			["ThoriumMod/Tracker"] = "Greaveil_JungleEastHouse",
			["ThoriumMod/WeaponMaster"] = "Castle1",
			["CalamityMod/Archmage"] = "R7_SnowMansion",
			["Terraria/Cyborg"] = "Ziiio_AirshipHouse",
			["Terraria/Mechanic"] = "Greaveil_SnowSteampunkManor",
			["Terraria/SantaClaus"] = "Aegis8_SnowCastle",
			["Terraria/TaxCollector"] = "Hannibal_SnowResidence",
			["CalamityMod/Bandit"] = "Aegis8_DesertOasisSettlement",
			["Terraria/ArmsDealer"] = "Greaveil_DesertPalace",
			["Terraria/DyeTrader"] = "Magoozy_Palace",
			["Terraria/Steampunker"] = "WoB_DesertPalace",
			["ThoriumMod/DesertAcolyte"] = "Aegis8_DesertSkyPalace",
			["CalValEX/Jharim"] = "Aegis8_JungleTreeCity",
			["Terraria/Dryad"] = "Huadiao_JungleGothicManor",
			["Terraria/Painter"] = "Greaveil_JungleWestHouse",
			["Terraria/WitchDoctor"] = "Ziiio_WitchDoctorHut",
			["ThoriumMod/Druid"] = "WoB_JungleTreehouse",
			["CalamityMod/SeaKing"] = "Greaveil_WestGalleon",
			["CalValEX/JellyPriestNPC"] = "WoB_HarborCenterHouse",
			["FishermanNPC/Fisherman"] = "Gnorp_MediumOceanHouse",
			["Terraria/Angler"] = "FishingPond1",
			["Terraria/Pirate"] = "Aegis8Showcase_CloudCity",
			["Terraria/Stylist"] = "Ziiio_BarberShop",
			["ThoriumMod/Diverman"] = "Greaveil_WestLighthouse",
			["UnuBattleRods/Fishylady"] = "WoB_HarborWestHouse",
			["MagicStorage/Golem"] = "MediumHouse1",
			["miningcracks_take_on_luiafk/Deeps"] = "Aegis8_CavernLakeManor",
			["Terraria/Clothier"] = "Aegis8_DesertGothicManor",
			["Terraria/Demolitionist"] = "Aegis8_CavernLakeSmithy",
			["Terraria/GoblinTinkerer"] = "SmallHouse1",
			["ThoriumMod/Blacksmith"] = "House3_Expination",
			["CalValEX/OracleNPC"] = "Huadiao_CrescentVillage",
			["Fargowiltas/Deviantt"] = "R7_PrincessCastle",
			["Terraria/DD2Bartender"] = "Ziiio_Tavern",
			["Terraria/Nurse"] = "Greaveil_SnowLodge",
			["Terraria/PartyGirl"] = "R7_ScarletDevilMansion",
			["Terraria/Princess"] = "Aegis8_HallowCliffManor",
			["Terraria/Wizard"] = "Greaveil_SpawnTreeTower",
			["ThoriumMod/Spiritualist"] = "Aegis8_FloatingPagoda",
			["WeaponEnchantments/Witch"] = "Aegis8_HallowRidgeVillage",
			["Terraria/Truffle"] = "Greaveil_MushroomSpire",
			["CalamityMod/BrimstoneWitch"] = "Huadiao_FireSkyVillage",
			["Fargowiltas/Abominationn"] = "Greaveil_UnderworldCastle"
		};
		if (entries.Count != NpcTownManifest.AuthoredCount)
			throw new InvalidOperationException($"Authored preset candidate matrix must contain exactly {NpcTownManifest.AuthoredCount} entries, found {entries.Count}.");
		HashSet<string> presetIds = new(StringComparer.Ordinal);
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!entries.TryGetValue(identity.NpcKey, out string presetId))
				throw new InvalidOperationException($"Authored preset candidate matrix is missing NPC: {identity.NpcKey}.");
			if (!AuthoredPresetCatalog.TryGet(presetId, out _))
				throw new InvalidOperationException($"Authored preset candidate does not exist: NPC={identity.NpcKey}; preset={presetId}.");
			if (!presetIds.Add(presetId))
				throw new InvalidOperationException($"Authored preset candidate is assigned more than once: {presetId}.");
		}

		return entries;
	}
}
