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
	public override void OnWorldUnload() => Reset();
	public override void SaveWorldData(TagCompound tag)
	{
		if (!HasReport)
			return;
		tag["ReportVersion"] = CurrentReportVersion;
		tag["SeedProfile"] = SeedProfile;
		tag["PlannedResidents"] = PlannedResidents;
		tag["StructuresBuilt"] = StructuresBuilt;
		tag["UniqueThemes"] = UniqueThemes;
		tag["ResidentsAssigned"] = ResidentsAssigned;
		tag["HousingValid"] = HousingValid;
		tag["RoomsDiscovered"] = RoomsDiscovered;
		tag["OwnerSignsPlaced"] = OwnerSignsPlaced;
		tag["RoadLinks"] = RoadLinks;
		tag["HousesPerDistrict"] = HousesPerDistrict;
		tag["ExpectedCivicHubs"] = ExpectedCivicHubs;
		tag["CivicHubsBuilt"] = CivicHubsBuilt;
		tag["CivicSignsPlaced"] = CivicSignsPlaced;
		tag["Districts"] = DistrictReports.Select(report => report.Save()).ToList();
		tag["Residences"] = ResidenceReports.Select(report => report.Save()).ToList();
	}

	public override void LoadWorldData(TagCompound tag)
	{
		Reset();
		if (!tag.ContainsKey("ReportVersion"))
			return;
		int reportVersion = tag.GetInt("ReportVersion");
		if (reportVersion <= 0 || reportVersion > CurrentReportVersion)
		{
			Mod.Logger.Warn($"Unsupported settlement report version {reportVersion}; audit data ignored.");
			return;
		}

		SeedProfile = tag.GetString("SeedProfile");
		PlannedResidents = tag.GetInt("PlannedResidents");
		StructuresBuilt = tag.GetInt("StructuresBuilt");
		UniqueThemes = tag.GetInt("UniqueThemes");
		ResidentsAssigned = tag.GetInt("ResidentsAssigned");
		HousingValid = tag.GetInt("HousingValid");
		RoomsDiscovered = tag.GetInt("RoomsDiscovered");
		OwnerSignsPlaced = tag.GetInt("OwnerSignsPlaced");
		RoadLinks = tag.GetInt("RoadLinks");
		HousesPerDistrict = tag.GetInt("HousesPerDistrict");
		if (reportVersion >= 2)
		{
			ExpectedCivicHubs = tag.GetInt("ExpectedCivicHubs");
			CivicHubsBuilt = tag.GetInt("CivicHubsBuilt");
			CivicSignsPlaced = tag.GetInt("CivicSignsPlaced");
		}

		if (tag.ContainsKey("Districts"))
		{
			foreach (TagCompound districtTag in tag.GetList<TagCompound>("Districts"))
			{
				if (SettlementDistrictReport.TryLoad(districtTag, out SettlementDistrictReport report))
					DistrictReports.Add(report);
			}
		}

		if (reportVersion >= 3 && tag.ContainsKey("Residences"))
		{
			HashSet<int> reviewIds = new();
			HashSet<string> npcKeys = new(StringComparer.Ordinal);
			foreach (TagCompound residenceTag in tag.GetList<TagCompound>("Residences"))
			{
				if (!SettlementResidenceReport.TryLoad(residenceTag, out SettlementResidenceReport report) || !report.IsCanonical() || !reviewIds.Add(report.ReviewId) || !npcKeys.Add(report.NpcKey))
				{
					Mod.Logger.Warn("Invalid or duplicate DingDong residence registry entry ignored.");
					continue;
				}

				ResidenceReports.Add(report);
			}

			ResidenceReports.Sort((left, right) => left.ReviewId.CompareTo(right.ReviewId));
		}

		HasReport = true;
		LoadedFromSave = true;
	}
}
