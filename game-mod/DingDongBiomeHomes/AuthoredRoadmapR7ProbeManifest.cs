using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Visually screened, standalone large residences from roadmap intake R7.
/// These private-home assets remain probe-only until dedicated-server housing,
/// NPC/biome/anchor and player visual gates pass.
/// </summary>
public static class AuthoredRoadmapR7ProbeManifest
{
	private static readonly AuthoredPreset[] Presets =
	{
		Create("R7_NadikiMansion", 300, 220, "Nadiki", "2456777286", "B7CCF635CA9F7C52340327AB5C13CEF1227FA6C263BBC381393CC9223976C651"),
		Create("R7_SnowMansion", 450, 350, "Steam creator 76561199008443415", "2461816371", "9C0BE7720DB17E00741B6713720F86803EE29956C95C470B61FC0DA2F64D1C93"),
		Create("R7_PrincessCastle", 600, 250, "Steam creator 76561198033511916", "3129951489", "FEF13461385019CE85F55320A505DD9B02B9A4FD18CE646894714AFD9F8FFF2F"),
		Create("R7_CastleCoolResidence", 700, 450, "Steam creator 76561198267237675", "2580275865", "C80F5EECD763FF03B67B20778DFA647BFECFF2E05945B68CF56E1D431286160A"),
		Create("R7_EsonoraHome", 500, 300, "Steam creator 76561198977544804", "2848278947", "A12FA73C9C52B04A6880414C80C8EB720FA57F3762DDF045126F66381C1A0881"),
		Create("R7_ScarletDevilMansion", 650, 350, "Steam creator 76561198081703326", "2445047728", "5A689C25DBF6EF97FCB1C7A298EDD3ECE06B36EDC14224E1F91C4F9A510E3086"),
		Create("R7_UnrealDeltaCastle", 600, 250, "Steam creator 76561198990976764", "3004032818", "3D6C08D781DE4FB10B3E7913751BF242E2CE0EDF0BDEAC63D45CDB15A91710A8"),
		Create("R7_MansionAlmost", 600, 300, "Steam creator 76561198382584470", "2493801540", "FF0ADFF5EE33E7A80E51FD549F8189AE3E53B48B5BAAD64453AB0447CB8007BB")
	};

	public static IReadOnlyList<AuthoredPreset> All => Presets;

	private static AuthoredPreset Create(
		string id,
		int width,
		int height,
		string author,
		string workshopId,
		string worldSha256) => new(
		id,
		$"Structures/PrivateHome/RoadmapR7/{id}.shstruct",
		width,
		height,
		null,
		0,
		author,
		$"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopId}",
		$"wld-sha256:{worldSha256}",
		AuthoredPresetCatalog.PrivateHomeLicense,
		PresetRole.Residence | PresetRole.Civic | PresetRole.PrivateHome,
		AuthoredPlacementMode.TerrainScene,
		false,
		false);
}
