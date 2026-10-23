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
	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(HasReport);
		if (!HasReport)
			return;
		writer.Write(CurrentReportVersion);
		writer.Write(SeedProfile);
		writer.Write(PlannedResidents);
		writer.Write(StructuresBuilt);
		writer.Write(UniqueThemes);
		writer.Write(ResidentsAssigned);
		writer.Write(HousingValid);
		writer.Write(RoomsDiscovered);
		writer.Write(OwnerSignsPlaced);
		writer.Write(RoadLinks);
		writer.Write(HousesPerDistrict);
		writer.Write(ExpectedCivicHubs);
		writer.Write(CivicHubsBuilt);
		writer.Write(CivicSignsPlaced);
		writer.Write((ushort)DistrictReports.Count);
		foreach (SettlementDistrictReport report in DistrictReports)
			report.NetSend(writer);
		writer.Write((ushort)ResidenceReports.Count);
		foreach (SettlementResidenceReport report in ResidenceReports)
			report.NetSend(writer);
	}

	public override void NetReceive(BinaryReader reader)
	{
		Reset();
		if (!reader.ReadBoolean())
			return;
		int reportVersion = reader.ReadInt32();
		if (reportVersion != CurrentReportVersion)
			throw new InvalidDataException($"Unsupported DingDong settlement report version {reportVersion}.");
		SeedProfile = reader.ReadString();
		PlannedResidents = reader.ReadInt32();
		StructuresBuilt = reader.ReadInt32();
		UniqueThemes = reader.ReadInt32();
		ResidentsAssigned = reader.ReadInt32();
		HousingValid = reader.ReadInt32();
		RoomsDiscovered = reader.ReadInt32();
		OwnerSignsPlaced = reader.ReadInt32();
		RoadLinks = reader.ReadInt32();
		HousesPerDistrict = reader.ReadInt32();
		ExpectedCivicHubs = reader.ReadInt32();
		CivicHubsBuilt = reader.ReadInt32();
		CivicSignsPlaced = reader.ReadInt32();
		int districtCount = reader.ReadUInt16();
		if (districtCount > NetworkHardeningLimits.MaxSettlementDistricts)
			throw new InvalidDataException("Settlement district count exceeds limit.");
		for (int index = 0; index < districtCount; index++)
			DistrictReports.Add(SettlementDistrictReport.NetReceive(reader));
		int residenceCount = reader.ReadUInt16();
		if (residenceCount > NetworkHardeningLimits.MaxSettlementResidences)
			throw new InvalidDataException("Settlement residence count exceeds limit.");
		for (int index = 0; index < residenceCount; index++)
		{
			SettlementResidenceReport report = SettlementResidenceReport.NetReceive(reader);
			if (!report.IsCanonical())
				throw new InvalidDataException($"Non-canonical DingDong residence registry entry received: {report.NpcKey}/{report.PresetId}.");
			ResidenceReports.Add(report);
		}

		ResidenceReports.Sort((left, right) => left.ReviewId.CompareTo(right.ReviewId));
		HasReport = true;
	}
}
