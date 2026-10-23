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
	internal static void CompleteCandidateGeneration(IReadOnlyList<CandidateDeploymentSite> sites, int uniquePresets, int housingValid, int roomsDiscovered)
	{
		DistrictReports.Clear();
		ResidenceReports.Clear();
		foreach (CandidateDeploymentSite site in sites)
		{
			if (!NpcTownManifest.TryGet(site.NpcKey, out NpcTownIdentity identity))
				throw new InvalidOperationException($"Generated residence is not in the canonical NPC ledger: {site.NpcKey}.");
			SettlementResidenceReport report = new(identity.Variant + 1, site.NpcKey, site.PresetId, site.Biome, site.AssetArea);
			if (!report.IsCanonical())
				throw new InvalidOperationException($"Generated residence identity is not canonical: {site.NpcKey}/{site.PresetId}.");
			ResidenceReports.Add(report);
		}

		ResidenceReports.Sort((left, right) => left.ReviewId.CompareTo(right.ReviewId));
		if (!HasCompleteResidenceRegistry)
			throw new InvalidOperationException($"Generated residence registry is incomplete or non-canonical: {ResidenceReports.Count}/{NpcTownManifest.AuthoredCount}.");
		int districtIndex = 0;
		foreach (IGrouping<HomeBiome, CandidateDeploymentSite> district in sites.GroupBy(item => item.Biome).OrderBy(group => group.Key))
		{
			List<CandidateDeploymentSite> residences = district.ToList();
			DistrictReports.Add(new SettlementDistrictReport(++districtIndex, district.Key, Envelope(residences.Select(item => item.AssetArea)), residences.Count, residences.Count));
		}

		StructuresBuilt = sites.Count;
		UniqueThemes = uniquePresets;
		ResidentsAssigned = sites.Count;
		HousingValid = housingValid;
		RoomsDiscovered = roomsDiscovered;
		OwnerSignsPlaced = 0;
		RoadLinks = 0;
		ExpectedCivicHubs = 0;
		CivicHubsBuilt = 0;
		CivicSignsPlaced = 0;
		HasReport = true;
	}
}
