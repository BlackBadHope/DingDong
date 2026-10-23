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
	private void RunRelocationPlanOrApply(CommandCaller caller, string worldFile, bool apply)
	{
		const string planPrefix = "DINGDONG_CHEST_RELOCATION_PLAN";
		const string applyPrefix = "DINGDONG_CHEST_RELOCATION_APPLY";
		string prefix = apply ? applyPrefix : planPrefix;
		Mod.Logger.Info($"{prefix}_BEGIN|world={worldFile}|npc={ScarletNpcKey}|apply={apply}|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}");
		if (!Worldgen.ContinentalWorldDimensions.IsDeploymentWorld || !Main.masterMode || !Main.getGoodWorld)
			throw new InvalidOperationException("Chest relocation probe requires a Large ForTheWorthy Legendary world.");
		if (NpcAuthoredPresetManifest.Count != 0)
			throw new InvalidOperationException($"Approved runtime manifest must remain empty during chest relocation probing: {NpcAuthoredPresetManifest.Count}/61.");
		if (!CandidateDeploymentSitePlanner.TryPlanAll(Mod, AuthoredCandidateDeploymentManifest.All, out List<CandidateDeploymentSite> sites, out string siteFailure))
		{
			ReplyFailure(caller, prefix, $"site-plan:{siteFailure}");
			return;
		}

		CandidateDeploymentSite scarletSite = null;
		foreach (CandidateDeploymentSite site in sites)
		{
			if (string.Equals(site.NpcKey, ScarletNpcKey, StringComparison.Ordinal))
			{
				scarletSite = site;
				break;
			}
		}

		if (scarletSite == null)
		{
			ReplyFailure(caller, prefix, "scarlet-site-not-found");
			return;
		}

		if (!PersistentChestRelocator.TryPlan(scarletSite, out ChestRelocationPlan plan, out string planFailure))
		{
			ReplyFailure(caller, prefix, planFailure);
			return;
		}

		if (!apply)
		{
			string planned = $"{prefix}_END|verdict=PASS|world={worldFile}|npc={ScarletNpcKey}|source={plan.SourceTopLeft.X},{plan.SourceTopLeft.Y}|destination={plan.DestinationTopLeft.X},{plan.DestinationTopLeft.Y}|tileType={plan.TileType}|style={plan.Style}|items={plan.ItemCount}|signature={plan.ItemSignature}|mutationApplied=False|approved={NpcAuthoredPresetManifest.Count}/61";
			Mod.Logger.Info(planned);
			caller.Reply(planned, Color.LightGreen);
			return;
		}

		if (!PersistentChestRelocator.TryApply(plan, out ChestRelocationReceipt receipt, out string applyFailure))
		{
			ReplyFailure(caller, prefix, applyFailure);
			return;
		}

		string applied = $"{prefix}_END|verdict=PASS|world={worldFile}|npc={ScarletNpcKey}|source={receipt.SourceTopLeft.X},{receipt.SourceTopLeft.Y}|destination={receipt.DestinationTopLeft.X},{receipt.DestinationTopLeft.Y}|destinationIndex={receipt.DestinationIndex}|items={receipt.ItemCount}|signature={receipt.ItemSignature}|reusedSourceSlot={receipt.ReusedSourceSlot}|mutationApplied=True|approved={NpcAuthoredPresetManifest.Count}/61";
		Mod.Logger.Info(applied);
		caller.Reply(applied, Color.LightGreen);
	}

	private bool TryFindCandidateSite(string npcKey, out CandidateDeploymentSite selected, out string failure)
	{
		selected = null;
		if (!CandidateDeploymentSitePlanner.TryPlanAll(Mod, AuthoredCandidateDeploymentManifest.All, out List<CandidateDeploymentSite> sites, out failure))
			return false;
		foreach (CandidateDeploymentSite site in sites)
		{
			if (string.Equals(site.NpcKey, npcKey, StringComparison.Ordinal))
			{
				selected = site;
				failure = string.Empty;
				return true;
			}
		}

		failure = $"site-not-found:{npcKey}";
		return false;
	}

	private static bool TryResolveCanonicalNpcKey(string input, out string npcKey)
	{
		foreach (AuthoredDeploymentProfile profile in AuthoredCandidateDeploymentManifest.All)
		{
			if (string.Equals(profile.NpcKey, input, StringComparison.OrdinalIgnoreCase))
			{
				npcKey = profile.NpcKey;
				return true;
			}
		}

		npcKey = string.Empty;
		return false;
	}
}
