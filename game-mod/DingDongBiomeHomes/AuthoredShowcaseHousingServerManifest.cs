using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Exact Terraria housing results for the Aegis8 showcase intake. Passing
/// scenes remain replacement candidates until NPC, biome and player visual
/// placement gates pass.
/// </summary>
public static class AuthoredShowcaseHousingServerManifest
{
	public const int CandidateCount = 5;
	public const int PassingCount = 3;
	public const int FailingCount = 2;
	public const string TmodSha256 = "BD7052897C4857B8AD1FF02C9497CBF641E88C892A955876CCF8F041B9F6ADE8";
	public const string ServerLogSha256 = "874EBFD6AFBFDC767A0A16F0F7AA46B2D77C5AA168930C8945B9A65A04CB979B";

	private static readonly Dictionary<string, int> PassingRooms = new(StringComparer.Ordinal)
	{
		["Aegis8Showcase_SnowHarborSettlement"] = 4,
		["Aegis8Showcase_MoonCitadel"] = 4,
		["Aegis8Showcase_CloudCity"] = 2
	};

	private static readonly HashSet<string> Failing = new(StringComparer.Ordinal)
	{
		"Aegis8Showcase_EastAirship",
		"Aegis8Showcase_OceanGalleon"
	};

	static AuthoredShowcaseHousingServerManifest()
	{
		if (PassingRooms.Count != PassingCount || Failing.Count != FailingCount)
			throw new InvalidOperationException("Showcase housing summary invariant failed.");
		if (AuthoredShowcaseReplacementProbeManifest.All.Count != CandidateCount)
			throw new InvalidOperationException($"Showcase housing requires {CandidateCount} candidates.");
		foreach (AuthoredPreset preset in AuthoredShowcaseReplacementProbeManifest.All)
		{
			if (PassingRooms.ContainsKey(preset.Id) == Failing.Contains(preset.Id))
				throw new InvalidOperationException($"Showcase housing result is missing or duplicated: {preset.Id}.");
		}
	}

	public static bool IsPassing(string presetId) => PassingRooms.ContainsKey(presetId);

	public static int ValidRoomCount(string presetId) => PassingRooms.TryGetValue(presetId, out int rooms) ? rooms : 0;
}
