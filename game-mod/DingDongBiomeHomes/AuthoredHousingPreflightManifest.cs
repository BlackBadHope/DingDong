using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Offline room-needs screening for the 61 authored NPC candidates. This is a
/// triage manifest, not Terraria housing acceptance: WorldGen.StartRoomCheck
/// in a dedicated-server placement remains authoritative.
/// </summary>
public static class AuthoredHousingPreflightManifest
{
	public const int CandidateCount = 61;
	public const int CandidatesWithAllRoomNeeds = 55;
	public const int CandidatesRequiringFurniture = 6;
	public const int DetectedRoomCount = 124;
	public const string AnalyzerSourceSha256 = "A431A9E56E7AECB5F7414B7BC84F8F92C8F28EEADABED36B7CA6BCBF437A1F75";

	private static readonly HashSet<string> FurnitureRequired = BuildFurnitureRequired();

	public static bool RequiresFurnitureSupplement(string presetId) => FurnitureRequired.Contains(presetId);

	private static HashSet<string> BuildFurnitureRequired()
	{
		HashSet<string> presetIds = new(StringComparer.Ordinal)
		{
			"Aegis8_MountainLodge",
			"Aegis8_SteampunkOrb",
			"Aegis8_HallowCliffManor",
			"WoB_HarborCenterHouse",
			"WoB_HarborWestHouse",
			"Aegis8_DesertGothicManor"
		};

		if (presetIds.Count != CandidatesRequiringFurniture)
			throw new InvalidOperationException($"Housing preflight repair set must contain {CandidatesRequiringFurniture} presets, found {presetIds.Count}.");

		HashSet<string> mappedPresetIds = new(StringComparer.Ordinal);
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(identity.NpcKey, out AuthoredPreset preset))
				throw new InvalidOperationException($"Housing preflight candidate did not resolve: {identity.NpcKey}.");
			mappedPresetIds.Add(preset.Id);
		}

		if (mappedPresetIds.Count != CandidateCount)
			throw new InvalidOperationException($"Housing preflight requires {CandidateCount} unique mapped presets, found {mappedPresetIds.Count}.");
		foreach (string presetId in presetIds)
		{
			if (!mappedPresetIds.Contains(presetId))
				throw new InvalidOperationException($"Housing preflight repair preset is not mapped to an NPC: {presetId}.");
		}
		if (mappedPresetIds.Count - presetIds.Count != CandidatesWithAllRoomNeeds)
			throw new InvalidOperationException("Housing preflight summary invariant failed.");

		return presetIds;
	}
}
