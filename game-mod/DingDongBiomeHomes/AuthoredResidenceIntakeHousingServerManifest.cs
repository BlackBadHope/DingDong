using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Exact Terraria housing results for the Huadiao/Barton residence intake.
/// Barton remains useful as a landmark but cannot be an NPC home unchanged.
/// </summary>
public static class AuthoredResidenceIntakeHousingServerManifest
{
	public const int CandidateCount = 5;
	public const int PassingCount = 4;
	public const int FailingCount = 1;
	public const string TmodSha256 = "5B77055049AC1D2485A859D64875E6E48D65468C4760C6EE77E2BC9DE9BBD543";
	public const string ServerLogSha256 = "73EDE4DC66EF70AFFB54B11F3161AB84AFD54DE28CE206AC881BEDACAFBC8E63";

	private static readonly Dictionary<string, int> PassingRooms = new(StringComparer.Ordinal)
	{
		["Huadiao_ElementalSkyHouse"] = 2,
		["Huadiao_FireSkyVillage"] = 5,
		["Huadiao_CrescentVillage"] = 2,
		["Huadiao_JungleGothicManor"] = 3
	};

	private static readonly HashSet<string> Failing = new(StringComparer.Ordinal)
	{
		"Barton_SkyTreehouse"
	};

	static AuthoredResidenceIntakeHousingServerManifest()
	{
		if (PassingRooms.Count != PassingCount || Failing.Count != FailingCount)
			throw new InvalidOperationException("Residence intake housing summary invariant failed.");
		if (AuthoredResidenceIntakeProbeManifest.All.Count != CandidateCount)
			throw new InvalidOperationException($"Residence intake housing requires {CandidateCount} candidates.");
		foreach (AuthoredPreset preset in AuthoredResidenceIntakeProbeManifest.All)
		{
			if (PassingRooms.ContainsKey(preset.Id) == Failing.Contains(preset.Id))
				throw new InvalidOperationException($"Residence intake housing result is missing or duplicated: {preset.Id}.");
		}
	}

	public static bool IsPassing(string presetId) => PassingRooms.ContainsKey(presetId);

	public static int ValidRoomCount(string presetId) => PassingRooms.TryGetValue(presetId, out int rooms) ? rooms : 0;
}
