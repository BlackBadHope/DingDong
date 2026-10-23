using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

internal static class ResidenceAcceptanceProbe
{
	internal static void Run(Mod mod)
	{
		List<string> failures = new();
		if (!SettlementWorldState.HasCompleteResidenceRegistry)
			failures.Add("incomplete-residence-registry");
		int valid = 0, nativeContainers = 0;
		foreach (var residence in SettlementWorldState.Residences)
		{
			var housing = AuthoredSettlementBuilder.ProbeHousing(residence.Area);
			if (housing.ValidRooms > 0) valid++;
			else failures.Add(residence.NpcKey + ":" + housing.FirstFailure);
			if (NpcAuthoredPresetManifest.TryGetCandidate(residence.NpcKey, out var preset) &&
				AuthoredSourceContext.HasRestoredReference(preset) &&
				residence.Area.Width == preset.Width && residence.Area.Height == preset.Height)
			{
				try { DingDongAuthoredStructureLoader.RequireNativeContainer(preset, residence.Area.Location); nativeContainers++; }
				catch (InvalidOperationException ex) { failures.Add(residence.NpcKey + ":native-container:" + ex.Message); }
			}
		}
		if (valid != 61) failures.Add($"habitable={valid}/61");
		failures.AddRange(ResidenceIntegrityAudit.ReadFailures(mod));
		foreach (string failure in failures)
			mod.Logger.Error("DINGDONG_RESIDENCE_ACCEPTANCE_FAILURE|" + failure);
		if (failures.Count != 0)
			throw new InvalidOperationException("Residence physical acceptance: " + string.Join("; ", failures));
		mod.Logger.Info($"DINGDONG_RESIDENCE_ACCEPTANCE|verdict=PASS|registry=61|habitable=61|loadedFromSave={SettlementWorldState.LoadedFromSave}|allHousesRechecked=True|restoredNativeContainers={nativeContainers}|visualApproval=False");
	}
}
