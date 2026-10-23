using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class ResidenceIntegrityAudit
{
	internal static IReadOnlyList<string> ReadFailures(Mod mod)
	{
		List<string> failures = new();
		foreach (var residence in SettlementWorldState.Residences)
		{
			try
			{
				if (!NpcAuthoredPresetManifest.TryGetCandidate(residence.NpcKey, out AuthoredPreset preset) ||
					!AuthoredCandidateDeploymentManifest.TryGet(residence.NpcKey, out var profile))
					throw new InvalidOperationException("Residence does not resolve to an authored asset");
				var result = DingDongAuthoredStructureLoader.InspectIntegrity(mod, preset, residence.Area.Location, profile);
				if (!result.Passed)
					failures.Add($"review={residence.ReviewId};npc={residence.NpcKey};preset={preset.Id};omitted={result.OmittedTiles}/{result.OmittedWalls};missingMasonry={result.MissingSolidConstruction};missingWalls={result.MissingHouseWalls};first={result.FirstFailure}");
			}
			catch (Exception ex) { failures.Add($"review={residence.ReviewId};reader-error={ex.Message}"); }
		}
		return failures;
	}
}
