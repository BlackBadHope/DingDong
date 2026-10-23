using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Private replacement intake for the sixteen failed housing candidates. Each
/// structure remains probe-only until it is explicitly imported into the runtime
/// catalog after Terraria housing and placement-mode review.
/// </summary>
public static class AuthoredReplacementProbeManifest
{
	public const string Source = "https://steamcommunity.com/sharedfiles/filedetails/?id=3653147194";
	public const string PackageSha256 = "5A8794B4E00454719B21DBE613C1F40EFF1AF31B9952F3EB8EEB9F69F77253C7";
	public const string License = "PRIVATE_HOME_ONLY; no redistribution permission recorded";

	private static readonly AuthoredPreset[] Presets =
	{
		Create("Gnorp_HeartHouse", "HeartHouse", 53, 53, PresetRole.Residence | PresetRole.Civic),
		Create("Gnorp_LargeValentineHouse", "LargeValentineHouse", 46, 41, PresetRole.Residence),
		Create("Gnorp_LargeTundraHouse", "LargeTundraHouse", 36, 35, PresetRole.Residence),
		Create("Gnorp_LargeHallowHouse", "LargeHallowHouse", 32, 47, PresetRole.Residence),
		Create("Gnorp_LargeMushroomHouse", "LargeMushroomHouse", 47, 61, PresetRole.Residence | PresetRole.Greenhouse),
		Create("Gnorp_LargeJungleHouse", "LargeJungleHouse", 34, 39, PresetRole.Residence | PresetRole.Greenhouse),
		Create("Gnorp_LargeSandstoneHouse", "LargeSandstoneHouse", 34, 35, PresetRole.Residence),
		Create("Gnorp_MediumValentineHouse", "MediumValentineHouse", 32, 32, PresetRole.Residence),
		Create("Gnorp_MediumTundraHouse", "MediumTundraHouse", 29, 29, PresetRole.Residence),
		Create("Gnorp_MediumHallowHouse", "MediumHallowHouse", 25, 40, PresetRole.Residence),
		Create("Gnorp_MediumMushroomHouse", "MediumMushroomHouse", 37, 46, PresetRole.Residence | PresetRole.Greenhouse),
		Create("Gnorp_MediumOceanHouse", "MediumOceanHouse", 26, 26, PresetRole.Residence | PresetRole.Harbor, AuthoredPlacementMode.HarborScene),
		Create("Gnorp_MediumSandstoneHouse", "MediumSandstoneHouse", 27, 27, PresetRole.Residence),
		Create("Gnorp_OldCabinMedium", "OldCabinMedium", 27, 24, PresetRole.Residence),
		Create("Gnorp_MediumCastle", "MediumCastle", 86, 76, PresetRole.Residence | PresetRole.Civic),
		Create("Gnorp_LargeCastle", "LargeCastle", 100, 90, PresetRole.Residence | PresetRole.Civic)
	};

	public static IReadOnlyList<AuthoredPreset> All => Presets;

	private static AuthoredPreset Create(
		string id,
		string fileName,
		int width,
		int height,
		PresetRole roles,
		AuthoredPlacementMode placementMode = AuthoredPlacementMode.SurfaceStructure) => PublicHomeReplacementCatalog.Create(id, width, height, placementMode, roles);
}
