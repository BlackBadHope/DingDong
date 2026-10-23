using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Read-only dedicated-server contract gate for candidate placement metadata.
/// It never places a tile and cannot enable the approved worldgen manifest.
/// </summary>
public sealed partial class DingDongDeploymentProbeCommand
{
	private void RunSimultaneousPlan(CommandCaller caller, string worldFile)
	{
		const string prefix = "DINGDONG_DEPLOYMENT_SIMULTANEOUS_PLAN";
		Mod.Logger.Info($"{prefix}_BEGIN|world={worldFile}|profiles={AuthoredCandidateDeploymentManifest.Count}|simultaneousLayout=True|size={Main.maxTilesX}x{Main.maxTilesY}|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}");
		if (!Worldgen.ContinentalWorldDimensions.IsDeploymentWorld || !Main.masterMode || !Main.getGoodWorld)
			throw new InvalidOperationException("Simultaneous deployment plan requires a Large ForTheWorthy Legendary world.");
		// StructureMap is generation-only state and is not retained by a normal
		// world load.  The generation pass persists its exact pre-residence map;
		// use that snapshot for a loaded-world read-only probe instead of treating
		// a missing GenVars.structures instance as an empty foreign-reservation map.
		if (Terraria.WorldBuilding.GenVars.structures == null)
		{
			CandidateDeploymentSitePlanner.ReplayReservationPlan(Mod);
			caller.Reply($"{prefix}_END|verdict=REPLAY_LOGGED|mutated=0|source=pre-residence-reservation-snapshot", Color.LightGreen);
			return;
		}
		if (!CandidateDeploymentSitePlanner.TryPlanSimultaneous(Mod, AuthoredCandidateDeploymentManifest.All, out List<CandidateDeploymentSite> sites, out string failure))
		{
			string failed = $"{prefix}_END|verdict=FAIL|planned=0/61|simultaneousLayout=True|failure={failure}|mutated=0|visualApproved={NpcAuthoredPresetManifest.Count}/61";
			Mod.Logger.Error(failed);
			caller.Reply(failed, Color.OrangeRed);
			return;
		}

		HashSet<string> npcs = new(StringComparer.Ordinal);
		HashSet<string> presets = new(StringComparer.Ordinal);
		int siteIndex = 0;
		foreach (CandidateDeploymentSite site in sites)
		{
			npcs.Add(site.NpcKey);
			presets.Add(site.PresetId);
			if (!NpcAuthoredPresetManifest.TryGetCandidate(site.NpcKey, out AuthoredPreset preset))
				throw new InvalidOperationException($"Candidate preset missing after simultaneous planning: {site.NpcKey}/{site.PresetId}.");
			siteIndex++;
			Mod.Logger.Info($"{prefix}|{AuthoredResidenceLayoutPolicy.FormatSiteLine(siteIndex, site, preset, sites)}");
		}

		if (!PersistentChestRelocator.TryPlanAll(sites, out List<ChestRelocationPlan> relocationPlans, out string relocationFailure))
			throw new InvalidOperationException($"Simultaneous layout relocation plans changed after admission: {relocationFailure}.");
		string summary = $"{prefix}_END|verdict=PASS|planned={sites.Count}/61|distinctNpcs={npcs.Count}|distinctPresets={presets.Count}|simultaneousLayout=True|losslessChestRelocationPlans={relocationPlans.Count}|unresolvedPersistentObjectOverlaps=0|mutated=0|technicalWorldgenReady=True|visualApproved={NpcAuthoredPresetManifest.Count}/61|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}";
		Mod.Logger.Info(summary);
		caller.Reply(summary, Color.LightGreen);
	}
}
