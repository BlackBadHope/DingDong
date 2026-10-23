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
	private static void AddImportedPresets(Dictionary<string, AuthoredPreset> entries)
	{
		// Sixteen independent, large authored residences which passed Terraria's
		// dedicated-server housing authority. They close the distinct candidate
		// inventory only; runtime placement remains fail-closed in the NPC manifest.
		AddImported(entries, AuthoredShowcaseReplacementProbeManifest.All, "Aegis8Showcase_SnowHarborSettlement");
		AddImported(entries, AuthoredShowcaseReplacementProbeManifest.All, "Aegis8Showcase_MoonCitadel");
		AddImported(entries, AuthoredShowcaseReplacementProbeManifest.All, "Aegis8Showcase_CloudCity");
		AddImported(entries, AuthoredResidenceIntakeProbeManifest.All, "Huadiao_ElementalSkyHouse");
		AddImported(entries, AuthoredResidenceIntakeProbeManifest.All, "Huadiao_FireSkyVillage");
		AddImported(entries, AuthoredResidenceIntakeProbeManifest.All, "Huadiao_CrescentVillage");
		AddImported(entries, AuthoredResidenceIntakeProbeManifest.All, "Huadiao_JungleGothicManor");
		AddImported(entries, AuthoredRoadmapR6ProbeManifest.All, "Hannibal_SnowResidence");
		AddImported(entries, AuthoredRoadmapR6ProbeManifest.All, "Magoozy_Palace");
		AddImported(entries, AuthoredRoadmapR7ProbeManifest.All, "R7_NadikiMansion");
		AddImported(entries, AuthoredRoadmapR7ProbeManifest.All, "R7_SnowMansion");
		AddImported(entries, AuthoredRoadmapR7ProbeManifest.All, "R7_PrincessCastle");
		AddImported(entries, AuthoredRoadmapR7ProbeManifest.All, "R7_CastleCoolResidence");
		AddImported(entries, AuthoredRoadmapR7ProbeManifest.All, "R7_EsonoraHome");
		AddImported(entries, AuthoredRoadmapR7ProbeManifest.All, "R7_ScarletDevilMansion");
		AddImported(entries, AuthoredRoadmapR7ProbeManifest.All, "R7_UnrealDeltaCastle");
		AddImported(entries, AuthoredReplacementProbeManifest.All, "Gnorp_MediumOceanHouse");
	}

	private static void AddCatalogPresets(Dictionary<string, AuthoredPreset> entries)
	{
		if (entries.Count != 105)
			throw new InvalidOperationException($"Authored preset inventory must contain 105 entries, found {entries.Count}.");
	}
}
