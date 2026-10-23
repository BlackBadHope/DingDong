using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Candidate-envelope search shared by the independent 3F preview gate and
/// normal world generation. Independent planning intentionally ignores other
/// DingDong scenes. Simultaneous planning broad-phases with every accepted
/// protected footprint, keeps each primary authored residence focus visually
/// separate, resolves sparse-envelope overlaps against the exact mutation
/// masks, and rejects persistent-object collisions before any tile mutation.
/// </summary>
public static partial class CandidateDeploymentSitePlanner
{
	private static int Rank(AuthoredResidencePlacementContract contract)
	{
		if (contract.ScaleClass == ResidenceScaleClass.Mega)
			return 0;
		if (contract.ScaleClass == ResidenceScaleClass.Landmark)
			return 1;
		if (contract.RequiresIsolation)
			return 2;
		return 3;
	}

	private static int CompareForSimultaneousLayout(AuthoredDeploymentProfile left, AuthoredDeploymentProfile right, IReadOnlyDictionary<string, AuthoredPreset> presets, int attempt)
	{
		int leftPriority = LayoutPriority(left, attempt);
		int rightPriority = LayoutPriority(right, attempt);
		int priority = leftPriority.CompareTo(rightPriority);
		if (priority != 0)
			return priority;
		long leftArea = (long)presets[left.NpcKey].Width * presets[left.NpcKey].Height;
		long rightArea = (long)presets[right.NpcKey].Width * presets[right.NpcKey].Height;
		int area = rightArea.CompareTo(leftArea);
		return area != 0 ? area : string.Compare(left.NpcKey, right.NpcKey, StringComparison.Ordinal);
	}

	private static int LayoutPriority(AuthoredDeploymentProfile profile, int attempt)
	{
		AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(profile.NpcKey);
		// Reserve the two scarce Hallow macro shelves before coastal candidates can
		// consume the eastern shelf's exact mutation space. Other Hallow contracts
		// remain in their normal constrained-biome phase after the harbors.
		if (contract.MacroWindow != ResidenceMacroWindow.Any)
			return -1000;
		int isolation = contract.RequiresIsolation ? 0 : 1;
		int scale = contract.ScaleClass switch
		{
			ResidenceScaleClass.Mega => 0,
			ResidenceScaleClass.Landmark => 1,
			ResidenceScaleClass.Large => 2,
			ResidenceScaleClass.Medium => 3,
			_ => 4
		};
		bool oceanSurface = profile.TargetBiome == HomeBiome.Ocean && profile.TargetAnchor != DeploymentTargetAnchor.SkyClearanceEnvelope;
		int anchor = profile.TargetAnchor switch
		{
			_ when oceanSurface || profile.TargetAnchor == DeploymentTargetAnchor.CoastalWaterlineEnvelope => 0,
			_ when profile.TargetBiome == HomeBiome.Hallow => 1,
			DeploymentTargetAnchor.OrganicMushroomCavernEnvelope => 2,
			DeploymentTargetAnchor.UnderworldAshLavaShelf => 3,
			DeploymentTargetAnchor.OrganicCavernEnvelope => 4,
			DeploymentTargetAnchor.SkyClearanceEnvelope when profile.TargetBiome == HomeBiome.Ocean => 5,
			DeploymentTargetAnchor.SkyClearanceEnvelope => 6,
			_ when profile.TargetBiome is HomeBiome.Snow or HomeBiome.Desert or HomeBiome.Jungle => 7,
			_ => 8
		};
		if (attempt % 4 == 2)
			return anchor * 10 + scale;
		if (attempt % 4 == 3)
			return ManifestOrder(profile.NpcKey);
		// Isolation must not pull the 550-wide ocean sky mega ahead of waterline
		// harbors. Constrained coasts are planned first; the sky mega then takes
		// the lighter shore.
		return anchor * 100 + isolation * 10 + scale;
	}

	private static int ManifestOrder(string npcKey)
	{
		if (NpcTownManifest.TryGet(npcKey, out NpcTownIdentity identity))
			return identity.Variant;
		return int.MaxValue;
	}
}
