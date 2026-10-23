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
	private void AuditDeploymentProfile(HashSet<string> presets, ref int compact, ref int scenes, ref long authoredCells, ref long interiorVoidCells, ref long boundaryExteriorCells, AuthoredDeploymentProfile profile, out AuthoredPreset preset, out AuthoredSceneMutationMask mutationMask, out AuthoredSceneMaskInfo mask)
	{
		if (!presets.Add(profile.PresetId))
			throw new InvalidOperationException($"Deployment preset is duplicated: {profile.PresetId}.");
		if (!NpcAuthoredPresetManifest.TryGetCandidate(profile.NpcKey, out preset) || !string.Equals(preset.Id, profile.PresetId, StringComparison.Ordinal))
			throw new InvalidOperationException($"Deployment profile does not match candidate mapping: {profile.NpcKey}/{profile.PresetId}.");
		Rectangle assetArea = new(100, 100, preset.Width, preset.Height);
		mutationMask = DingDongAuthoredStructureLoader.InspectMutationMask(Mod, preset, profile);
		Rectangle protectedArea = mutationMask.ProtectedFootprint(assetArea.Location, profile.ProtectedPadding);
		if (protectedArea.Width <= mutationMask.MutationBounds.Width || protectedArea.Height <= mutationMask.MutationBounds.Height)
			throw new InvalidOperationException($"Deployment protected footprint is not larger than the integrated mutation bounds: {preset.Id}.");
		if (protectedArea.Width >= Main.maxTilesX || protectedArea.Height >= Main.maxTilesY)
			throw new InvalidOperationException($"Deployment protected footprint cannot fit the target world: {preset.Id}={protectedArea.Width}x{protectedArea.Height}.");
		mask = DingDongAuthoredStructureLoader.InspectSceneMask(Mod, preset);
		if (mask.Width != preset.Width || mask.Height != preset.Height || mask.AuthoredContentCells <= 0)
			throw new InvalidOperationException($"Deployment mask is invalid: {preset.Id}.");
		authoredCells += mask.AuthoredContentCells;
		interiorVoidCells += mask.InteriorVoidCells;
		boundaryExteriorCells += mask.BoundaryConnectedExteriorCells;
		if (profile.BoundaryPolicy == DeploymentBoundaryPolicy.ExactCompactFootprint)
			compact++;
		else
			scenes++;
	}

	private static string FormatOptionalBounds(Rectangle? bounds)
	{
		if (!bounds.HasValue)
			return "none";
		Rectangle value = bounds.Value;
		return $"{value.X},{value.Y},{value.Width},{value.Height}";
	}
}
