using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes;
public sealed partial class BiomeHomeWorldgenSystem
{
	private void PlaceResidenceCandidate(GenerationProgress progress,
		Dictionary<Point16, TileAnchorSnapshot> tileEntityAnchors, ref int structuresPlaced,
		ref int housingValid, ref int roomsDiscovered, HashSet<string> uniquePresets,
		CandidateDeploymentSite site)
	{
		if (!NpcAuthoredPresetManifest.TryGetCandidate(site.NpcKey, out AuthoredPreset preset) ||
			!string.Equals(preset.Id, site.PresetId, StringComparison.Ordinal))
			throw new InvalidOperationException(
				$"DingDong worldgen candidate mismatch: {site.NpcKey}/{site.PresetId}.");
		if (!uniquePresets.Add(preset.Id))
			throw new InvalidOperationException(
				$"DingDong worldgen duplicated authored preset: {preset.Id}.");
		if (!AuthoredCandidateDeploymentManifest.TryGet(site.NpcKey,
			out AuthoredDeploymentProfile profile))
			throw new InvalidOperationException(
				$"DingDong worldgen deployment profile missing: {site.NpcKey}/{site.PresetId}.");
		using var frameTrace = DingDongAuthoredStructureLoader.TraceCloudFraming(Mod, preset, site.AssetArea.Location);
		DingDongAuthoredStructureLoader.Generate(Mod, preset, site.AssetArea.Location, profile);
		VerifyTileEntityAnchors(tileEntityAnchors, site.AssetArea, preset.Id);
		HousingProbeResult housing = AuthoredSettlementBuilder.ProbeHousing(site.AssetArea);
		DingDongAuthoredStructureLoader.RequireIntegrity(Mod, preset, site.AssetArea.Location, profile, "post-framing");
		if (housing.ValidRooms <= 0)
			throw new InvalidOperationException($"Authored residence lost housing validity during simultaneous worldgen: npc={site.NpcKey}; preset={preset.Id}; area={site.AssetArea.X},{site.AssetArea.Y},{site.AssetArea.Width},{site.AssetArea.Height}; failure={housing.FirstFailure}.");

		structuresPlaced++;
		housingValid++;
		roomsDiscovered += housing.ValidRooms;
		Mod.Logger.Info($"DINGDONG_WORLDGEN_RESIDENCE|index={structuresPlaced}/61|npc={site.NpcKey}|preset={preset.Id}|biome={site.Biome}|area={site.AssetArea.X},{site.AssetArea.Y},{site.AssetArea.Width},{site.AssetArea.Height}|validRooms={housing.ValidRooms}|collisionResolved={site.PersistentObjectOverlaps > 0}");
		progress.Set(structuresPlaced / (double)NpcTownManifest.AuthoredCount);
	}
}
