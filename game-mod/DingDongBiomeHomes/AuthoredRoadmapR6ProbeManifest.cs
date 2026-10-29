using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Visually screened standalone residences from roadmap intake R6. These are
/// private-home probe assets only: no NPC assignment or worldgen placement is
/// allowed until dedicated-server housing and player visual gates pass.
/// </summary>
public static class AuthoredRoadmapR6ProbeManifest
{
	private const string HannibalSource = "https://steamcommunity.com/sharedfiles/filedetails/?id=3358990936";
	private const string HannibalWorldSha256 = "5B7F4B18182429FA69CC4155CF5A146771BD87FF1E276D026126474200CBC00E";
	private const string MagoozySource = "https://steamcommunity.com/sharedfiles/filedetails/?id=2459200280";
	private const string MagoozyWorldSha256 = "53F688539A9AC7139AEF1878F5C462EE77B4438A79775B9841857641B91C750F";

	private static readonly AuthoredPreset[] Presets =
	{
		Create(
			"Hannibal_SnowResidence",
			"Hannibal",
			290,
			120,
			"HANNIBAL",
			HannibalSource,
			HannibalWorldSha256,
			PresetRole.Residence | PresetRole.Civic),
		Create(
			"Magoozy_Palace",
			"Magoozy",
			405,
			225,
			"SallyMagoozy",
			MagoozySource,
			MagoozyWorldSha256,
			PresetRole.Residence | PresetRole.Civic)
	};

	public static IReadOnlyList<AuthoredPreset> All => Presets;

	private static AuthoredPreset Create(
		string id,
		string folder,
		int width,
		int height,
		string author,
		string source,
		string worldSha256,
		PresetRole roles) => new(
		id,
		$"Structures/PrivateHome/{folder}/{id}.shstruct",
		width,
		height,
		null,
		0,
		author,
		source,
		$"wld-sha256:{worldSha256}",
		AuthoredPresetCatalog.PrivateHomeLicense,
		roles | PresetRole.PrivateHome,
		AuthoredPlacementMode.TerrainScene,
		false,
		false);
}
