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
	private void RunSitePlan(CommandCaller caller, string worldFile)
	{
		const string prefix = "DINGDONG_DEPLOYMENT_SITE_PLAN";
		const string scope = "independent-single-scene";
		Mod.Logger.Info($"{prefix}_BEGIN|world={worldFile}|profiles={AuthoredCandidateDeploymentManifest.Count}|scope={scope}|simultaneousLayout=False|size={Main.maxTilesX}x{Main.maxTilesY}|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}");
		if (!Worldgen.ContinentalWorldDimensions.IsDeploymentWorld || !Main.masterMode || !Main.getGoodWorld)
			throw new InvalidOperationException("Deployment site plan requires a Large ForTheWorthy Legendary world.");
		if (NpcAuthoredPresetManifest.Count != 0)
			throw new InvalidOperationException($"Approved runtime manifest must remain empty during site planning: {NpcAuthoredPresetManifest.Count}/61.");
		if (!CandidateDeploymentSitePlanner.TryPlanAll(Mod, AuthoredCandidateDeploymentManifest.All, out List<CandidateDeploymentSite> sites, out string failure))
		{
			string failed = $"{prefix}_END|verdict=FAIL|planned={sites.Count}/61|scope={scope}|simultaneousLayout=False|failure={failure}|approved={NpcAuthoredPresetManifest.Count}/61";
			Mod.Logger.Error(failed);
			caller.Reply(failed, Color.OrangeRed);
			return;
		}

		HashSet<string> presets = new(StringComparer.Ordinal);
		HashSet<string> npcs = new(StringComparer.Ordinal);
		int cleanProfiles = 0;
		int advisoryProfiles = 0;
		int persistentObjectOverlaps = 0;
		foreach (CandidateDeploymentSite site in sites)
		{
			if (!presets.Add(site.PresetId) || !npcs.Add(site.NpcKey))
				throw new InvalidOperationException($"Deployment site plan duplicates an assignment: {site.NpcKey}/{site.PresetId}.");
			persistentObjectOverlaps += site.PersistentObjectOverlaps;
			if (site.PersistentObjectOverlaps == 0)
				cleanProfiles++;
			else
				advisoryProfiles++;
			Mod.Logger.Info($"{prefix}|npc={site.NpcKey}|preset={site.PresetId}|biome={site.Biome}|area={site.AssetArea.X},{site.AssetArea.Y},{site.AssetArea.Width},{site.AssetArea.Height}|protected={site.ProtectedArea.X},{site.ProtectedArea.Y},{site.ProtectedArea.Width},{site.ProtectedArea.Height}|reference={site.TargetReference.X},{site.TargetReference.Y}|fallback={site.Fallback}|persistentObjectOverlaps={site.PersistentObjectOverlaps}|persistentCollisions={site.PersistentCollisionSummary}");
		}

		string summary = $"{prefix}_END|verdict=PASS|planned={sites.Count}/61|distinctNpcs={npcs.Count}|distinctPresets={presets.Count}|scope={scope}|simultaneousLayout=False|cleanProfiles={cleanProfiles}|persistentAdvisoryProfiles={advisoryProfiles}|persistentObjectOverlaps={persistentObjectOverlaps}|worldgenPlacementApproved=False|approved={NpcAuthoredPresetManifest.Count}/61|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}";
		Mod.Logger.Info(summary);
		caller.Reply(summary, Color.LightGreen);
	}
}
