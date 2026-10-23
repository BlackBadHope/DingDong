using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Exact dedicated-server housing results for roadmap intake R6.
/// Technical housing PASS does not grant NPC assignment or visual placement approval.
/// </summary>
public static class AuthoredRoadmapR6HousingServerManifest
{
	public const int CandidateCount = 2;
	public const int PassingCount = 2;
	public const string TmodSha256 = "42BBF5D3525922B0A879D65B3EAAC163716564A4422E41FFA056D9EBB2098262";
	public const string ServerLogSha256 = "C8F1A53BD91A392AC4819D6AF576C3322487430892DDE15585FABD205527DBC3";

	private static readonly Dictionary<string, int> PassingRooms = new(StringComparer.Ordinal)
	{
		["Hannibal_SnowResidence"] = 3,
		["Magoozy_Palace"] = 8
	};

	static AuthoredRoadmapR6HousingServerManifest()
	{
		if (PassingRooms.Count != CandidateCount || PassingRooms.Count != PassingCount)
			throw new InvalidOperationException("Roadmap R6 housing summary invariant failed.");
		if (AuthoredRoadmapR6ProbeManifest.All.Count != CandidateCount)
			throw new InvalidOperationException($"Roadmap R6 housing requires {CandidateCount} candidates.");
		foreach (AuthoredPreset preset in AuthoredRoadmapR6ProbeManifest.All)
		{
			if (!PassingRooms.ContainsKey(preset.Id))
				throw new InvalidOperationException($"Roadmap R6 housing result is missing: {preset.Id}.");
		}
	}

	public static bool IsPassing(string presetId) => PassingRooms.ContainsKey(presetId);

	public static int ValidRoomCount(string presetId) => PassingRooms.TryGetValue(presetId, out int rooms) ? rooms : 0;
}
