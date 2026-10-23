using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;

/// <summary>
/// Current mapped candidates still requiring a dedicated-server housing repair.
/// Failed candidates from the first baseline were replaced by server-validated
/// authored alternatives, so the active repair set is intentionally empty.
/// </summary>
public static class AuthoredHousingServerBaselineManifest
{
	public const int CandidateCount = 61;
	public const int PassingCount = 61;
	public const int RepairCount = 0;
	public const int NumericSeed = 284875780;
	public const string TmodSha256 = "061A8B8AE1073A301FBBA08520E24032911B4D681A466D8256D3F8F675D3BBC1";

	private static readonly HashSet<string> RepairPresetIds = BuildRepairPresetIds();

	public static bool RequiresRepair(string presetId) => RepairPresetIds.Contains(presetId);

	private static HashSet<string> BuildRepairPresetIds()
	{
		HashSet<string> presetIds = new(StringComparer.Ordinal);

		if (presetIds.Count != RepairCount)
			throw new InvalidOperationException($"Server housing repair set must contain {RepairCount} presets, found {presetIds.Count}.");

		int mapped = 0;
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(identity.NpcKey, out AuthoredPreset preset))
				throw new InvalidOperationException($"Server housing baseline candidate did not resolve: {identity.NpcKey}.");
			if (presetIds.Contains(preset.Id))
				mapped++;
		}
		if (mapped != RepairCount || CandidateCount - mapped != PassingCount)
			throw new InvalidOperationException("Server housing baseline summary invariant failed.");

		return presetIds;
	}
}
