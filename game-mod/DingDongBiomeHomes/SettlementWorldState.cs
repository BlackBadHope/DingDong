using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DingDongBiomeHomes;
public sealed partial class SettlementWorldState : ModSystem
{
	// V4 appends authoritative quest coordinates to every residence on the wire.
	// Tagged saves remain backwards-readable; V3 network packets must not be
	// interpreted with the longer V4 layout.
	private const int CurrentReportVersion = 4;
	private static readonly List<SettlementDistrictReport> DistrictReports = new();
	private static readonly List<SettlementResidenceReport> ResidenceReports = new();
	public static bool HasReport { get; private set; }
	public static string SeedProfile { get; private set; } = string.Empty;
	public static int PlannedResidents { get; private set; }
	public static int StructuresBuilt { get; private set; }
	public static int UniqueThemes { get; private set; }
	public static int ResidentsAssigned { get; private set; }
	public static int HousingValid { get; private set; }
	public static int RoomsDiscovered { get; private set; }
	public static int OwnerSignsPlaced { get; private set; }
	public static int RoadLinks { get; private set; }
	public static int HousesPerDistrict { get; private set; }
	public static int ExpectedCivicHubs { get; private set; }
	public static int CivicHubsBuilt { get; private set; }
	public static int CivicSignsPlaced { get; private set; }
	public static IReadOnlyList<SettlementDistrictReport> Districts => DistrictReports;
	public static IReadOnlyList<SettlementResidenceReport> Residences => ResidenceReports;
	public static bool LoadedFromSave { get; private set; }
	public static bool HasCompleteResidenceRegistry => ResidenceReports.Count == NpcTownManifest.AuthoredCount && ResidenceReports.Select(report => report.ReviewId).Distinct().Count() == NpcTownManifest.AuthoredCount && ResidenceReports.All(report => report.IsCanonical());
}
