using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;
public sealed record AuthoredDeploymentProfile(string NpcKey, string PresetId, HomeBiome TargetBiome, AuthoredPlacementMode SourceMode, DeploymentSourceAnchor SourceAnchor, int? SourceGroundOffset, DeploymentTargetAnchor TargetAnchor, DeploymentBoundaryPolicy BoundaryPolicy, int ProtectedPadding, int BlendMargin, DeploymentSeedFallback SeedFallback, bool PlayerVisualApproved)
{
	public Rectangle ProtectedFootprint(Rectangle assetArea) => new(assetArea.X - ProtectedPadding, assetArea.Y - ProtectedPadding, assetArea.Width + ProtectedPadding * 2, assetArea.Height + ProtectedPadding * 2);
}
