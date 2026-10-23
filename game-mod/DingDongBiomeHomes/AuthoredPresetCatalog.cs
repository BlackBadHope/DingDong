using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
/// <summary>
/// Immutable inventory of third-party authored geometry embedded in the mod.
/// This is deliberately separate from the NPC manifest: a theme name or a set
/// of procedural parameters is not a building preset.
/// </summary>
public static partial class AuthoredPresetCatalog
{
	public const string StructifySource = "https://github.com/Valks-Terraria-Mods/Structify";
	public const string StructifyCommit = "eea570727f9b4037be02782911407d68915516c2";
	public const string StructifyLicense = "MIT";
	public const string StructifyCatalogSha256 = "AF06A212BAE01B1D6764396987F9BDF6AA282D7813FE44DBBA995B40E20BEFA7";
	public const string ZiiioSource = "https://steamcommunity.com/sharedfiles/filedetails/?id=2441486368";
	public const string ZiiioWorldSha256 = "D1AB3865D130180BB1AFC5F5F4E6595B9958D3D985B9F4C4C838CC90328C1D72";
	public const string WorldOfBuildingsSource = "https://steamcommunity.com/sharedfiles/filedetails/?id=2441025960";
	public const string WorldOfBuildingsWorldSha256 = "86C0A4BFCE01A8C39E9C4E9095FE5AFEE62B823DF4AABC2DC83D978D4669919E";
	public const string GreaveilSource = "https://steamcommunity.com/sharedfiles/filedetails/?id=3669025349";
	public const string GreaveilWorldSha256 = "B5738D5DA7C8065B069F7EC7DB076D3712E5F9584594F582F1FD7AE10B3FB418";
	public const string Aegis8Source = "https://steamcommunity.com/sharedfiles/filedetails/?id=2789072123";
	public const string Aegis8WorldSha256 = "F14FE6860EB3811E74A4E7657EBDE3B3EAC819311F6614DEA85C16736FA6008C";
	public const string PrivateHomeLicense = "PRIVATE_HOME_ONLY; no redistribution permission recorded";
	private static readonly Dictionary<string, AuthoredPreset> Entries = BuildEntries();
	public static IReadOnlyDictionary<string, AuthoredPreset> All => Entries;
}
