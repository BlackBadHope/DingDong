using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	internal static void DiagnoseTerrain(Mod mod, AuthoredDeploymentProfile profile, bool evaluateReservations = false)
	{
		if (!NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out AuthoredPreset preset))
			throw new InvalidOperationException("Missing candidate: " + profile.NpcKey);
		var mask = DingDongAuthoredStructureLoader.InspectMutationMask(mod, preset, profile);
		var index = SurfaceColumnIndex.Build();
		var masks = new Dictionary<string, AuthoredSceneMutationMask> { [profile.NpcKey] = mask };
		for (int salt = 1; salt <= CandidatePoolLayoutSalts; salt++)
		{
			int admitted = 0, rejected = 0;
			string example = "";
			bool[] allowed = Worldgen.ContinentalPlacementMaskPolicy.CreateStructureOnly();
			if (evaluateReservations) ActiveRankedCollector = RankedCandidateCollector.ConstraintPool(candidate => {
				if (AuthoredResidenceStructureReservation.CanReserve(candidate, allowed)) { admitted++; return true; }
				rejected++;
				if (example.Length == 0) example = $"candidate={candidate.ProtectedArea};overlaps={ResidenceReservationDiagnostics.Describe(candidate.ProtectedArea)}";
				return false;
			});
			try
			{
				bool found = TryPlan(profile, preset, mask, new List<Rectangle>(),
					Array.Empty<CandidateDeploymentSite>(), masks, index, salt, true,
					out CandidateDeploymentSite site, out string reason);
				mod.Logger.Info($"DINGDONG_TERRAIN_PROBE|npc={profile.NpcKey}|salt={salt}|found={found}|area={site?.AssetArea}|reason={reason}|reservationChecked={evaluateReservations}|admitted={admitted}|rejected={rejected}|example={example}|worldMutated=False");
			}
			finally { ActiveRankedCollector = null; }
		}
	}
}
