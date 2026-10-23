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
	private void RunPreview(CommandCaller caller, string worldFile, string npcKey)
	{
		const string prefix = "DINGDONG_DEPLOYMENT_PREVIEW";
		if (!TryResolveCanonicalNpcKey(npcKey, out string canonicalNpcKey))
		{
			ReplyFailure(caller, prefix, $"candidate-not-found:{npcKey}");
			return;
		}

		npcKey = canonicalNpcKey;
		Mod.Logger.Info($"{prefix}_BEGIN|world={worldFile}|npc={npcKey}|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}");
		if (PreviewWorldState.PreviewPlaced)
		{
			ReplyFailure(caller, prefix, $"single-use-world-already-has-preview:{PreviewWorldState.NpcKey}/{PreviewWorldState.PresetId}");
			return;
		}

		if (!Worldgen.ContinentalWorldDimensions.IsDeploymentWorld || !Main.masterMode || !Main.getGoodWorld)
			throw new InvalidOperationException("Visual preview requires a Large ForTheWorthy Legendary world.");
		if (NpcAuthoredPresetManifest.Count != 0)
			throw new InvalidOperationException($"Approved runtime manifest must remain empty during player preview: {NpcAuthoredPresetManifest.Count}/61.");
		if (!AuthoredCandidateDeploymentManifest.TryGet(npcKey, out AuthoredDeploymentProfile profile) || !NpcAuthoredPresetManifest.TryGetCandidate(npcKey, out AuthoredPreset preset) || !string.Equals(profile.PresetId, preset.Id, StringComparison.Ordinal))
		{
			ReplyFailure(caller, prefix, $"candidate-not-found:{npcKey}");
			return;
		}

		if (!TryFindCandidateSite(npcKey, out CandidateDeploymentSite site, out string siteFailure))
		{
			ReplyFailure(caller, prefix, siteFailure);
			return;
		}

		int relocatedChests = 0;
		if (site.PersistentObjectOverlaps > 0)
		{
			if (!PersistentChestRelocator.TryPlan(site, out ChestRelocationPlan relocationPlan, out string relocationPlanFailure))
			{
				ReplyFailure(caller, prefix, $"collision-resolution:{relocationPlanFailure}");
				return;
			}

			if (!PersistentChestRelocator.TryApply(relocationPlan, out ChestRelocationReceipt _, out string relocationApplyFailure))
			{
				ReplyFailure(caller, prefix, $"collision-resolution:{relocationApplyFailure}");
				return;
			}

			relocatedChests = 1;
			if (!TryFindCandidateSite(npcKey, out site, out siteFailure) || site.PersistentObjectOverlaps != 0)
			{
				ReplyFailure(caller, prefix, $"post-relocation-site:{siteFailure}:overlaps={site?.PersistentObjectOverlaps ?? -1}");
				return;
			}
		}

		AuthoredSceneMutationMask mutationMask = DingDongAuthoredStructureLoader.InspectMutationMask(Mod, preset, profile);
		AuthoredTileSnapshot[] snapshot = SnapshotTiles(site.AssetArea);
		using var containers = AuthoredNativeContainerTransaction.Begin();
		try
		{
			DingDongAuthoredStructureLoader.Generate(Mod, preset, site.AssetArea.Location, profile);
			HousingProbeResult housing = AuthoredSettlementBuilder.ProbeHousing(site.AssetArea);
			if (housing.ValidRooms <= 0)
			{
				RestoreTiles(site.AssetArea, snapshot);
				ReplyFailure(caller, prefix, $"housing-failed:{housing.FirstFailure}");
				return;
			}

			PreviewWorldState.Commit(npcKey, preset.Id, site.AssetArea, housing.ValidRooms);
			string summary = $"{prefix}_END|verdict=PASS|world={worldFile}|npc={npcKey}|preset={preset.Id}|biome={site.Biome}|area={site.AssetArea.X},{site.AssetArea.Y},{site.AssetArea.Width},{site.AssetArea.Height}|protected={site.ProtectedArea.X},{site.ProtectedArea.Y},{site.ProtectedArea.Width},{site.ProtectedArea.Height}|mutationCells={mutationMask.MutationCellCount}|validRooms={housing.ValidRooms}|relocatedChests={relocatedChests}|blendApplied=False|previewStateCommitted=True|saveRequired=True|playerVisualApproved=False|worldgenPlacementApproved=False|approved={NpcAuthoredPresetManifest.Count}/61";
			Mod.Logger.Info(summary);
			caller.Reply(summary, Color.LightGreen);
			containers.Commit();
		}
		catch (Exception exception)
		{
			RestoreTiles(site.AssetArea, snapshot);
			ReplyFailure(caller, prefix, $"{exception.GetType().Name}:{exception.Message.Replace('|', '/')}");
		}
	}
}
