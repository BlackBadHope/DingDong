using System;
using System.Collections.Generic;
using System.Linq;

namespace DingDongBiomeHomes;

public sealed partial class DingDongHousingProbeCommand
{
	private static List<AuthoredPreset> SelectPresets(HousingProbeRequest request)
	{
		List<AuthoredPreset> candidates = new();
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(identity.NpcKey, out AuthoredPreset preset))
				throw new InvalidOperationException($"Housing probe candidate did not resolve: {identity.NpcKey}.");
			candidates.Add(preset);
		}

		HashSet<string> candidateIds = new(candidates.Select(preset => preset.Id), StringComparer.Ordinal);
		return request.Mode switch
		{
			HousingProbeMode.RoadmapR7 => AuthoredRoadmapR7ProbeManifest.All.ToList(),
			HousingProbeMode.RoadmapR6 => AuthoredRoadmapR6ProbeManifest.All.ToList(),
			HousingProbeMode.Intake => AuthoredResidenceIntakeProbeManifest.All.ToList(),
			HousingProbeMode.Showcase => AuthoredShowcaseReplacementProbeManifest.All.ToList(),
			HousingProbeMode.Replacements => AuthoredReplacementProbeManifest.All.ToList(),
			HousingProbeMode.Reserve => AuthoredPresetCatalog.All.Values.Where(preset => !candidateIds.Contains(preset.Id) && !preset.ContainsNbt).ToList(),
			HousingProbeMode.FailedShells => candidates.Where(preset => AuthoredHousingServerBaselineManifest.RequiresRepair(preset.Id)).ToList(),
			_ => candidates
		};
	}

	private static void ValidatePresetSelection(HousingProbeRequest request, List<AuthoredPreset> presets)
	{
		if (request.Mode == HousingProbeMode.Reserve && presets.Count != 18)
			throw new InvalidOperationException($"Housing reserve probe requires exactly 18 non-NBT presets, found {presets.Count}.");
		if (request.Mode == HousingProbeMode.FailedShells && presets.Count != AuthoredHousingServerBaselineManifest.RepairCount)
			throw new InvalidOperationException($"Failed-shell probe requires exactly {AuthoredHousingServerBaselineManifest.RepairCount} presets, found {presets.Count}.");
		if (request.Mode is HousingProbeMode.Replacements or HousingProbeMode.Showcase or HousingProbeMode.Intake or HousingProbeMode.RoadmapR6 or HousingProbeMode.RoadmapR7 && presets.Count == 0)
			throw new InvalidOperationException($"{request.Mode} housing probe requires at least one preset.");
	}
}
