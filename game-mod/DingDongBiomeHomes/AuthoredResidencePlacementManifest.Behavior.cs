using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DingDongBiomeHomes;
/// <summary>
/// Spatial deployment metadata for the 61 authored residences.
/// This is deliberately separate from <see cref = "NpcTownManifest"/> lore and
/// from the approved visual runtime manifest. NPC identity biome remains an
/// allowed lore constraint, while the first allowed biome is the deployment
/// preference chosen for the authored asset.
/// </summary>
public static partial class AuthoredResidencePlacementManifest
{
	public static bool TryGet(string npcKey, out AuthoredResidencePlacementContract contract) => ByNpc.TryGetValue(npcKey, out contract);
	public static AuthoredResidencePlacementContract Required(string npcKey)
	{
		if (!ByNpc.TryGetValue(npcKey, out AuthoredResidencePlacementContract contract))
			throw new InvalidOperationException($"Residence placement contract missing: {npcKey}");
		return contract;
	}

	private static HomeBiome[] Biomes(params HomeBiome[] biomes) => biomes;
	private static ResidenceAltitudeBand[] Layers(params ResidenceAltitudeBand[] layers) => layers;
	private static void Add(Dictionary<string, AuthoredResidencePlacementContract> entries, string npcKey, ResidenceKind kind, ResidenceScaleClass scale, HomeBiome[] allowedBiomes, ResidenceAltitudeBand[] allowedLayers, ResidenceAltitudeBand preferredLayer, ResidenceAnchorMode anchor, string naturalContext, ResidenceClusterPolicy cluster, int landmarkWeight, int minimumVisualDistance, int maximumLocalDensity, int cadenceWeight, ResidenceMacroWindow macroWindow = ResidenceMacroWindow.Any)
	{
		if (!NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
			throw new InvalidOperationException($"Residence placement contract NPC is not in the town manifest: {npcKey}.");
		if (!NpcAuthoredPresetManifest.TryGetCandidate(npcKey, out AuthoredPreset preset))
			throw new InvalidOperationException($"Residence placement contract NPC has no candidate preset: {npcKey}.");
		if (allowedBiomes == null || allowedBiomes.Length == 0)
			throw new InvalidOperationException($"Residence placement contract has no allowed biomes: {npcKey}.");
		if (allowedLayers == null || allowedLayers.Length == 0)
			throw new InvalidOperationException($"Residence placement contract has no allowed layers: {npcKey}.");
		if (Array.IndexOf(allowedBiomes, identity.Biome) < 0)
			throw new InvalidOperationException($"Residence placement allowed biomes must include the identity biome: {npcKey}.");
		if (Array.IndexOf(allowedLayers, preferredLayer) < 0)
			throw new InvalidOperationException($"Residence placement preferred layer must be allowed: {npcKey}.");
		if (macroWindow != ResidenceMacroWindow.Any && (identity.Biome != HomeBiome.Hallow || preferredLayer != ResidenceAltitudeBand.Surface))
		{
			throw new InvalidOperationException($"Residence macro window is incompatible with biome/layer: {npcKey}/{macroWindow}.");
		}

		bool canShare = cluster is ResidenceClusterPolicy.Hamlet or ResidenceClusterPolicy.Port or ResidenceClusterPolicy.TownEdge;
		bool requiresIsolation = cluster == ResidenceClusterPolicy.Isolated || scale == ResidenceScaleClass.Mega || (scale == ResidenceScaleClass.Landmark && cluster == ResidenceClusterPolicy.Isolated);
		AuthoredResidencePlacementContract contract = new(preset.Id, npcKey, kind, scale, new ReadOnlyCollection<HomeBiome>(allowedBiomes), allowedBiomes[0], new ReadOnlyCollection<ResidenceAltitudeBand>(allowedLayers), preferredLayer, anchor, naturalContext, cluster, landmarkWeight, minimumVisualDistance, preferredLayer, maximumLocalDensity, canShare && !requiresIsolation, requiresIsolation, cadenceWeight, macroWindow);
		if (!entries.TryAdd(npcKey, contract))
			throw new InvalidOperationException($"Duplicate residence placement contract: {npcKey}");
	}
}
