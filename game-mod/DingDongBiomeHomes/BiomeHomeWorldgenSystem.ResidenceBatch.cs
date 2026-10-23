using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
public sealed partial class BiomeHomeWorldgenSystem
{
	private int ApplyRelocationsAndPlaceResidences(GenerationProgress progress, List<CandidateDeploymentSite> sites, List<ChestRelocationPlan> relocationPlans, int relocatedChests, out int structuresPlaced, out int housingValid, out int roomsDiscovered, out HashSet<string> uniquePresets)
	{
		foreach (ChestRelocationPlan relocationPlan in relocationPlans)
		{
			if (!PersistentChestRelocator.TryApply(relocationPlan, out ChestRelocationReceipt receipt, out string relocationApplyFailure))
				throw new InvalidOperationException($"DingDong lossless collision apply failed: source={relocationPlan.SourceTopLeft.X},{relocationPlan.SourceTopLeft.Y}; destination={relocationPlan.DestinationTopLeft.X},{relocationPlan.DestinationTopLeft.Y}; {relocationApplyFailure}.");
			relocatedChests++;
			Mod.Logger.Info($"DINGDONG_WORLDGEN_CHEST_RELOCATION|source={receipt.SourceTopLeft.X},{receipt.SourceTopLeft.Y}|destination={receipt.DestinationTopLeft.X},{receipt.DestinationTopLeft.Y}|items={receipt.ItemCount}|signature={receipt.ItemSignature}|reusedSourceSlot={receipt.ReusedSourceSlot}");
		}

		progress.Message = "Building 61 authored biome residences";
		Dictionary<Point16, TileAnchorSnapshot> tileEntityAnchors = SnapshotTileEntityAnchors();
		structuresPlaced = 0;
		housingValid = 0;
		roomsDiscovered = 0;
		uniquePresets = new(StringComparer.Ordinal);
		foreach (CandidateDeploymentSite site in sites)
		{
			PlaceResidenceCandidate(progress, tileEntityAnchors, ref structuresPlaced, ref housingValid, ref roomsDiscovered, uniquePresets, site);
		}

		if (structuresPlaced != NpcTownManifest.AuthoredCount || housingValid != NpcTownManifest.AuthoredCount || uniquePresets.Count != NpcTownManifest.AuthoredCount)
		{
			throw new InvalidOperationException($"DingDong authored worldgen acceptance failed: structures={structuresPlaced}/61; housing={housingValid}/61; uniquePresets={uniquePresets.Count}/61.");
		}

		SettlementWorldState.CompleteCandidateGeneration(sites, uniquePresets.Count, housingValid, roomsDiscovered);
		return relocatedChests;
	}
}
