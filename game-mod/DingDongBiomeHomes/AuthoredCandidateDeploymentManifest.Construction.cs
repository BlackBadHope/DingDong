using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;
/// <summary>
/// Technical deployment contracts for the 61 distinct candidate assignments.
/// This is deliberately separate from the approved runtime manifest: none of
/// these profiles authorizes worldgen before player visual acceptance.
/// </summary>
public static partial class AuthoredCandidateDeploymentManifest
{
	private static Dictionary<string, AuthoredDeploymentProfile> BuildProfiles()
	{
		Dictionary<string, AuthoredDeploymentProfile> profiles = new(StringComparer.Ordinal);
		HashSet<string> presetIds = new(StringComparer.Ordinal);
		foreach (NpcTownIdentity identity in NpcTownManifest.All)
		{
			if (!NpcAuthoredPresetManifest.TryGetCandidate(identity.NpcKey, out AuthoredPreset preset))
				throw new InvalidOperationException($"Candidate deployment profile did not resolve: {identity.NpcKey}.");
			if (preset.ContainsNbt)
				throw new InvalidOperationException($"Candidate deployment profile cannot admit NBT: {identity.NpcKey}/{preset.Id}.");
			if (!presetIds.Add(preset.Id))
				throw new InvalidOperationException($"Candidate deployment preset is assigned more than once: {preset.Id}.");
			DeploymentSourceAnchor sourceAnchor = preset.PlacementReviewed && preset.GroundOffset.HasValue ? DeploymentSourceAnchor.ReviewedGroundRow : DeploymentSourceAnchor.AuthoredSceneBounds;
			if (sourceAnchor == DeploymentSourceAnchor.ReviewedGroundRow && preset.PlacementMode != AuthoredPlacementMode.SurfaceStructure)
				throw new InvalidOperationException($"Only compact SurfaceStructure assets may use a reviewed ground row: {preset.Id}.");
			if (sourceAnchor == DeploymentSourceAnchor.AuthoredSceneBounds && preset.PlacementMode == AuthoredPlacementMode.SurfaceStructure)
				throw new InvalidOperationException($"Compact SurfaceStructure is missing its reviewed ground row: {preset.Id}.");
			HomeBiome targetBiome = ResolveTargetBiome(identity.NpcKey,
				identity.Biome, preset.PlacementMode);
			DeploymentTargetAnchor targetAnchor = ResolveTargetAnchor(identity.NpcKey, targetBiome, preset.PlacementMode);
			if (preset.PlacementMode == AuthoredPlacementMode.TerrainScene &&
				targetAnchor == DeploymentTargetAnchor.OrganicCavernEnvelope)
				throw new InvalidOperationException(
					$"TerrainScene cannot preserve its natural boundary in an open cavern: {identity.NpcKey}/{preset.Id}.");
			DeploymentBoundaryPolicy boundaryPolicy = sourceAnchor == DeploymentSourceAnchor.ReviewedGroundRow ? DeploymentBoundaryPolicy.ExactCompactFootprint : DeploymentBoundaryPolicy.PreserveBoundaryConnectedExterior;
			(int protectedPadding, int blendMargin) = ResolveMargins(targetAnchor);
			DeploymentSeedFallback seedFallback = targetBiome switch
			{
				HomeBiome.Hallow => DeploymentSeedFallback.PuritySurfaceForPreHardmodeHallow,
				HomeBiome.Ocean => DeploymentSeedFallback.EitherNaturalOceanCoast,
				_ => DeploymentSeedFallback.ExactBiomeOnly
			};
			AuthoredDeploymentProfile profile = new(identity.NpcKey, preset.Id, targetBiome, preset.PlacementMode, sourceAnchor, preset.GroundOffset, targetAnchor, boundaryPolicy, protectedPadding, blendMargin, seedFallback, false);
			if (!profiles.TryAdd(identity.NpcKey, profile))
				throw new InvalidOperationException($"Duplicate candidate deployment profile: {identity.NpcKey}.");
		}

		if (profiles.Count != NpcTownManifest.AuthoredCount)
			throw new InvalidOperationException($"Candidate deployment manifest must contain {NpcTownManifest.AuthoredCount} profiles, found {profiles.Count}.");
		return profiles;
	}
}
