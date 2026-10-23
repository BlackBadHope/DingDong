using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Visually accepted standalone residences from the current private Workshop
/// intake. They are deliberately probe-only until Terraria accepts at least one
/// housing room and the player separately accepts their in-world placement.
/// </summary>
public static class AuthoredResidenceIntakeProbeManifest
{
	private const string HuadiaoSource = "https://steamcommunity.com/sharedfiles/filedetails/?id=2883756316";
	private const string HuadiaoWorldSha256 = "54BB112A3A9FDF6E8F948662B6A12AFB7352D30E4A4CF542BC315B16BE339D3E";
	private const string BartonSource = "https://steamcommunity.com/sharedfiles/filedetails/?id=2865484106";
	private const string BartonWorldSha256 = "F0D09B348AB0AC8938ADF0D4D1F60CD464A1D3D6906B91394F2EEA6C9BB48251";

	private static readonly AuthoredPreset[] Presets =
	{
		CreateHuadiao("Huadiao_ElementalSkyHouse", 190, 160, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Civic),
		CreateHuadiao("Huadiao_FireSkyVillage", 170, 165, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Civic),
		CreateHuadiao("Huadiao_CrescentVillage", 220, 220, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Civic),
		CreateHuadiao("Huadiao_JungleGothicManor", 330, 180, AuthoredPlacementMode.TerrainScene, PresetRole.Residence | PresetRole.Civic | PresetRole.Greenhouse),
		CreateBarton("Barton_SkyTreehouse", 280, 200, AuthoredPlacementMode.FloatingScene, PresetRole.Residence | PresetRole.Greenhouse)
	};

	public static IReadOnlyList<AuthoredPreset> All => Presets;

	private static AuthoredPreset CreateHuadiao(
		string id,
		int width,
		int height,
		AuthoredPlacementMode placementMode,
		PresetRole roles) => PublicHomeReplacementCatalog.Create(id, width, height, placementMode, roles);

	private static AuthoredPreset CreateBarton(
		string id,
		int width,
		int height,
		AuthoredPlacementMode placementMode,
		PresetRole roles) => PublicHomeReplacementCatalog.Create(id, width, height, placementMode, roles);
}
