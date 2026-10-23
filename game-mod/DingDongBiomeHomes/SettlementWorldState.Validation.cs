using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class SettlementWorldState
{
	public static IReadOnlyList<string> BuildAuditLines()
	{
		if (!HasReport)
			return new[]
			{
				"DingDong audit: this world has no settlement report (legacy world or generation disabled)."
			};
		bool pass = PlannedResidents > 0 && StructuresBuilt == PlannedResidents && UniqueThemes == PlannedResidents && ResidentsAssigned == PlannedResidents && HousingValid == PlannedResidents && CivicHubsBuilt == ExpectedCivicHubs && DistrictReports.Count > 0;
		string biomeSummary = string.Join(", ", DistrictReports.GroupBy(report => report.Biome).OrderBy(group => group.Key).Select(group => $"{group.Key}:{group.Sum(report => report.Structures)} houses/{group.Count()} districts"));
		return new[]
		{
			$"DingDong audit: {(pass ? "PASS" : "FAIL")}; profile={SeedProfile}; houses={StructuresBuilt}/{PlannedResidents}; themes={UniqueThemes}; residents={ResidentsAssigned}; housing={HousingValid}.",
			$"Districts={DistrictReports.Count}; housesPerDistrict={HousesPerDistrict}; rooms={RoomsDiscovered}; reviewTargets={ResidenceReports.Count}/{NpcTownManifest.AuthoredCount}; signs={OwnerSignsPlaced}; roadLinks={RoadLinks}; civicHubs={CivicHubsBuilt}/{ExpectedCivicHubs}; civicSigns={CivicSignsPlaced}.",
			$"Biomes: {biomeSummary}"};
	}
}
