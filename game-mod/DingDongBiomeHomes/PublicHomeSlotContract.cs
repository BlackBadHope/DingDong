using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>Numerical slot targets from text manifests, never a claim that new rooms passed Terraria.</summary>
public static class PublicHomeSlotContract
{
	private static readonly IReadOnlyDictionary<string, int> Declared = new Dictionary<string, int>(StringComparer.Ordinal)
	{
		["Gnorp_HeartHouse"] = 3, ["Gnorp_LargeValentineHouse"] = 4,
		["Gnorp_LargeTundraHouse"] = 3, ["Gnorp_LargeHallowHouse"] = 3,
		["Gnorp_LargeMushroomHouse"] = 3, ["Gnorp_LargeJungleHouse"] = 3,
		["Gnorp_LargeSandstoneHouse"] = 3, ["Gnorp_MediumValentineHouse"] = 2,
		["Gnorp_MediumTundraHouse"] = 2, ["Gnorp_MediumHallowHouse"] = 2,
		["Gnorp_MediumMushroomHouse"] = 2, ["Gnorp_MediumOceanHouse"] = 2,
		["Gnorp_MediumSandstoneHouse"] = 2, ["Gnorp_OldCabinMedium"] = 2,
		["Huadiao_ElementalSkyHouse"] = 2, ["Huadiao_FireSkyVillage"] = 5,
		["Huadiao_CrescentVillage"] = 2, ["Huadiao_JungleGothicManor"] = 3,
		["Hannibal_SnowResidence"] = 3, ["Magoozy_Palace"] = 8,
		["R7_NadikiMansion"] = 6, ["R7_SnowMansion"] = 8,
		["R7_PrincessCastle"] = 11, ["R7_CastleCoolResidence"] = 18,
		["R7_EsonoraHome"] = 10, ["R7_ScarletDevilMansion"] = 19,
		["R7_UnrealDeltaCastle"] = 13, ["R7_MansionAlmost"] = 9,
		["Aegis8Showcase_SnowHarborSettlement"] = 4,
		["Aegis8Showcase_MoonCitadel"] = 4, ["Aegis8Showcase_CloudCity"] = 2
	};

	public static int Required(string id, int fallback) => Declared.TryGetValue(id, out int rooms) ? rooms : fallback;
}
