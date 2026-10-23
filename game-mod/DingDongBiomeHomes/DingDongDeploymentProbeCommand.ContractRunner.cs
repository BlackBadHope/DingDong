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
	private void RunDeploymentContract(CommandCaller caller, string worldFile)
	{
		const string prefix = "DINGDONG_DEPLOYMENT_CONTRACT";
		Mod.Logger.Info($"{prefix}_BEGIN|world={worldFile}|profiles={AuthoredCandidateDeploymentManifest.Count}|size={Main.maxTilesX}x{Main.maxTilesY}|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}");
		if (!Worldgen.ContinentalWorldDimensions.IsDeploymentWorld || !Main.masterMode || !Main.getGoodWorld)
			throw new InvalidOperationException("Deployment contract requires a Large ForTheWorthy Legendary world.");
		if (!AuthoredCandidateDeploymentManifest.IsComplete)
			throw new InvalidOperationException($"Deployment profile manifest is incomplete: {AuthoredCandidateDeploymentManifest.Count}/{NpcTownManifest.AuthoredCount}.");
		if (NpcAuthoredPresetManifest.Count != 0)
			throw new InvalidOperationException($"Approved runtime manifest must remain empty during candidate deployment review: {NpcAuthoredPresetManifest.Count}/61.");
		HashSet<string> presets = new(StringComparer.Ordinal);
		int compact = 0;
		int scenes = 0;
		long authoredCells = 0;
		long interiorVoidCells = 0;
		long boundaryExteriorCells = 0;
		string summary = AuditDeploymentProfiles(prefix, presets, ref compact, ref scenes, ref authoredCells, ref interiorVoidCells, ref boundaryExteriorCells);
		caller.Reply(summary, Color.LightGreen);
	}

	private string AuditDeploymentProfiles(string prefix, HashSet<string> presets, ref int compact, ref int scenes, ref long authoredCells, ref long interiorVoidCells, ref long boundaryExteriorCells)
	{
		foreach (AuthoredDeploymentProfile profile in AuthoredCandidateDeploymentManifest.All)
		{
			AuthoredPreset preset;
			AuthoredSceneMutationMask mutationMask;
			AuthoredSceneMaskInfo mask;
			AuditDeploymentProfile(presets, ref compact, ref scenes, ref authoredCells, ref interiorVoidCells, ref boundaryExteriorCells, profile, out preset, out mutationMask, out mask);
			Mod.Logger.Info($"{prefix}|npc={profile.NpcKey}|preset={profile.PresetId}|biome={profile.TargetBiome}|sourceMode={profile.SourceMode}|sourceAnchor={profile.SourceAnchor}|targetAnchor={profile.TargetAnchor}|boundary={profile.BoundaryPolicy}|padding={profile.ProtectedPadding}|blend={profile.BlendMargin}|fallback={profile.SeedFallback}|asset={preset.Width}x{preset.Height}|mutationCells={mutationMask.MutationCellCount}|mutationBounds={mutationMask.MutationBounds.X},{mutationMask.MutationBounds.Y},{mutationMask.MutationBounds.Width},{mutationMask.MutationBounds.Height}|focusBounds={FormatOptionalBounds(mutationMask.FocusBounds)}|discardedMutationCells={mutationMask.DiscardedMutationCellCount}|terrainAnchorY={mutationMask.TerrainAnchorY?.ToString() ?? "none"}|maskContent={mask.AuthoredContentCells}|maskInteriorVoid={mask.InteriorVoidCells}|maskBoundaryExterior={mask.BoundaryConnectedExteriorCells}|playerVisual={profile.PlayerVisualApproved}");
		}

		string summary = $"{prefix}_END|verdict=PASS|profiles={AuthoredCandidateDeploymentManifest.Count}|distinct={presets.Count}|compact={compact}|scenes={scenes}|maskContent={authoredCells}|maskInteriorVoid={interiorVoidCells}|maskBoundaryExterior={boundaryExteriorCells}|approved={NpcAuthoredPresetManifest.Count}/61|masterMode={Main.masterMode}|getGoodWorld={Main.getGoodWorld}";
		Mod.Logger.Info(summary);
		return summary;
	}
}
