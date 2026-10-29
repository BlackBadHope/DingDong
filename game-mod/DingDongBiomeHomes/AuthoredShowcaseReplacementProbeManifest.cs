using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Large, non-overlapping Aegis8 showcase scenes selected by the visual gate.
/// They remain probe-only until Terraria housing validity is measured.
/// </summary>
public static class AuthoredShowcaseReplacementProbeManifest
{
	private static readonly AuthoredPreset[] Presets =
	{
		Create("Aegis8Showcase_SnowHarborSettlement", "snow-harbor-settlement", 240, 190, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Harbor),
		Create("Aegis8Showcase_MoonCitadel", "moon-citadel", 300, 330, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic),
		Create("Aegis8Showcase_EastAirship", "east-airship", 190, 190, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Harbor),
		Create("Aegis8Showcase_CloudCity", "cloud-city", 550, 300, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Harbor),
		Create("Aegis8Showcase_OceanGalleon", "ocean-galleon", 245, 230, AuthoredPlacementMode.HarborScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Harbor)
	};

	public static IReadOnlyList<AuthoredPreset> All => Presets;

	private static AuthoredPreset Create(
		string id,
		string fileName,
		int width,
		int height,
		AuthoredPlacementMode placementMode,
		PresetRole roles) => new(
		id,
		$"Structures/PrivateHome/Aegis8Showcase/{fileName}.shstruct",
		width,
		height,
		null,
		0,
		"TerraMonolith",
		AuthoredPresetCatalog.Aegis8Source,
		$"wld-sha256:{AuthoredPresetCatalog.Aegis8WorldSha256}",
		AuthoredPresetCatalog.PrivateHomeLicense,
		roles | PresetRole.PrivateHome,
		placementMode,
		false,
		false);
}
