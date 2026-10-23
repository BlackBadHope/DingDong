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
	public override void ClearWorld() => Reset();
	private static void Reset()
	{
		HasReport = false;
		SeedProfile = string.Empty;
		PlannedResidents = 0;
		StructuresBuilt = 0;
		UniqueThemes = 0;
		ResidentsAssigned = 0;
		HousingValid = 0;
		RoomsDiscovered = 0;
		OwnerSignsPlaced = 0;
		RoadLinks = 0;
		HousesPerDistrict = 0;
		ExpectedCivicHubs = 0;
		CivicHubsBuilt = 0;
		CivicSignsPlaced = 0;
		DistrictReports.Clear();
		ResidenceReports.Clear();
		LoadedFromSave = false;
	}
}
