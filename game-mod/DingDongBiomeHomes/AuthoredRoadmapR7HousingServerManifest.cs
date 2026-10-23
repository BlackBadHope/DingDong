using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Exact Terraria housing results for roadmap intake R7 on the disposable
/// Large ForTheWorthy Legendary dedicated-server world. Passing assets remain
/// candidate-only until NPC, biome, anchor and player visual gates pass.
/// </summary>
public static class AuthoredRoadmapR7HousingServerManifest
{
	public const int CandidateCount = 8;
	public const int PassingCount = 8;
	public const int FailingCount = 0;
	public const string TmodSha256 = "021EA9F2F1123229E8A805B9A7F9BDED51AB9EB1216BC64A7F4BC43FB48831FA";
	public const string ServerLogSha256 = "F4015EE1EDBA0E15F685F3FC4F1F6A2BEA5FD32765F3DED20B6345BA53938905";

	private static readonly Dictionary<string, int> PassingRooms = new(StringComparer.Ordinal)
	{
		["R7_NadikiMansion"] = 6,
		["R7_SnowMansion"] = 8,
		["R7_PrincessCastle"] = 11,
		["R7_CastleCoolResidence"] = 18,
		["R7_EsonoraHome"] = 10,
		["R7_ScarletDevilMansion"] = 19,
		["R7_UnrealDeltaCastle"] = 13,
		["R7_MansionAlmost"] = 9
	};

	static AuthoredRoadmapR7HousingServerManifest()
	{
		if (PassingRooms.Count != PassingCount)
			throw new InvalidOperationException("Roadmap R7 housing summary invariant failed.");
		if (AuthoredRoadmapR7ProbeManifest.All.Count != CandidateCount)
			throw new InvalidOperationException($"Roadmap R7 housing requires {CandidateCount} candidates.");
		foreach (AuthoredPreset preset in AuthoredRoadmapR7ProbeManifest.All)
		{
			if (!PassingRooms.ContainsKey(preset.Id))
				throw new InvalidOperationException($"Roadmap R7 housing result is missing: {preset.Id}.");
		}
	}

	public static bool IsPassing(string presetId) => PassingRooms.ContainsKey(presetId);

	public static int ValidRoomCount(string presetId) => PassingRooms.TryGetValue(presetId, out int rooms) ? rooms : 0;
}
