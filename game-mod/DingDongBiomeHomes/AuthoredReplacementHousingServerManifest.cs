using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Exact Terraria housing results for the first Gnorp replacement intake. A
/// passing entry is still only a visual-gate candidate, never an NPC assignment.
/// </summary>
public static class AuthoredReplacementHousingServerManifest
{
	public const int CandidateCount = 16;
	public const int PassingCount = 14;
	public const int FailingCount = 2;
	public const string TmodSha256 = "1F943A486DA8C32BCC914FF4C5E0BD697AC08629D3AA0368E81C289A8C212034";

	private static readonly Dictionary<string, int> PassingRooms = new(StringComparer.Ordinal)
	{
		["Gnorp_HeartHouse"] = 3,
		["Gnorp_LargeValentineHouse"] = 4,
		["Gnorp_LargeTundraHouse"] = 3,
		["Gnorp_LargeHallowHouse"] = 3,
		["Gnorp_LargeMushroomHouse"] = 3,
		["Gnorp_LargeJungleHouse"] = 3,
		["Gnorp_LargeSandstoneHouse"] = 3,
		["Gnorp_MediumValentineHouse"] = 2,
		["Gnorp_MediumTundraHouse"] = 2,
		["Gnorp_MediumHallowHouse"] = 2,
		["Gnorp_MediumMushroomHouse"] = 2,
		["Gnorp_MediumOceanHouse"] = 2,
		["Gnorp_MediumSandstoneHouse"] = 2,
		["Gnorp_OldCabinMedium"] = 2
	};

	private static readonly HashSet<string> Failing = new(StringComparer.Ordinal)
	{
		"Gnorp_MediumCastle",
		"Gnorp_LargeCastle"
	};

	static AuthoredReplacementHousingServerManifest()
	{
		if (PassingRooms.Count != PassingCount || Failing.Count != FailingCount)
			throw new InvalidOperationException("Replacement housing summary invariant failed.");
		foreach (AuthoredPreset preset in AuthoredReplacementProbeManifest.All)
		{
			if (PassingRooms.ContainsKey(preset.Id) == Failing.Contains(preset.Id))
				throw new InvalidOperationException($"Replacement housing result is missing or duplicated: {preset.Id}.");
		}
		if (AuthoredReplacementProbeManifest.All.Count != CandidateCount)
			throw new InvalidOperationException($"Replacement housing requires {CandidateCount} candidates.");
	}

	public static bool IsPassing(string presetId) => PassingRooms.ContainsKey(presetId);

	public static int ValidRoomCount(string presetId) => PassingRooms.TryGetValue(presetId, out int rooms) ? rooms : 0;
}
