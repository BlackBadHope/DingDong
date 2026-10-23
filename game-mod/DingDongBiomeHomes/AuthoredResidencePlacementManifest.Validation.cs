using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
/// <summary>
/// Spatial deployment metadata for the 61 authored residences.
/// This is deliberately separate from <see cref = "NpcTownManifest"/> lore and
/// from the approved visual runtime manifest. NPC identity biome remains an
/// allowed lore constraint, while the first allowed biome is the deployment
/// preference chosen for the authored asset.
/// </summary>
public static partial class AuthoredResidencePlacementManifest
{
	private static Dictionary<string, AuthoredResidencePlacementContract> BuildContracts()
	{
		Dictionary<string, AuthoredResidencePlacementContract> entries = new(StringComparer.Ordinal);
		AddForestAlchemyPlacements(entries);
		AddForestFargoVanillaPlacements(entries);
		AddForestThoriumPlacements(entries);
		AddSnowCorePlacements(entries);
		AddDesertPlacements(entries);
		AddJunglePlacements(entries);
		AddOceanPlacements(entries);
		AddUndergroundPlacements(entries);
		AddHallowCorePlacements(entries);
		AddSnowHallowClinicPlacement(entries);
		AddHallowRoyalPlacements(entries);
		AddForestWizardPlacement(entries);
		AddHallowModdedPlacements(entries);
		AddMushroomPlacements(entries);
		AddUnderworldPlacements(entries);
		ValidatePlacementCount(entries);
		ValidatePlacementCoverage(entries);
		return entries;
	}

	private static void ValidatePlacementCount(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		if (entries.Count != NpcTownManifest.AuthoredCount)
			throw new InvalidOperationException($"Residence placement manifest must contain {NpcTownManifest.AuthoredCount} contracts, found {entries.Count}.");
	}

	private static void ValidatePlacementCoverage(Dictionary<string, AuthoredResidencePlacementContract> entries)
	{
		HashSet<string> presets = new(StringComparer.Ordinal);
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!entries.TryGetValue(identity.NpcKey, out AuthoredResidencePlacementContract contract))
				throw new InvalidOperationException($"Residence placement contract missing NPC: {identity.NpcKey}.");
			if (!NpcAuthoredPresetManifest.TryGetCandidate(identity.NpcKey, out AuthoredPreset preset) || !string.Equals(preset.Id, contract.PresetId, StringComparison.Ordinal))
				throw new InvalidOperationException($"Residence placement contract preset mismatch: {identity.NpcKey}.");
			if (!presets.Add(contract.PresetId))
				throw new InvalidOperationException($"Residence placement contract duplicated preset: {contract.PresetId}.");
			if (contract.PreferredBiome != contract.AllowedBiomes[0])
				throw new InvalidOperationException($"Residence placement preferred biome must be the first allowed biome: {identity.NpcKey}.");
		}
	}
}
