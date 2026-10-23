using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;
/// <summary>
/// Deterministic spatial scoring and fail-closed distribution acceptance.
/// Collision clearance stays at 12 tiles; visual spacing is a separate, larger
/// constraint derived from authored focus size and scale class.
/// </summary>
public static partial class AuthoredResidenceLayoutPolicy
{
	public static BroadLayoutLayer BroadLayerOf(AuthoredResidencePlacementContract contract) => contract.BroadLayer;
	public static bool IsLandmarkOrMega(AuthoredResidencePlacementContract contract) => contract.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega;
	public static bool RequiresProtectedSeparation(AuthoredResidencePlacementContract contract) => contract.RequiresIsolation || contract.ClusterPolicy == ResidenceClusterPolicy.Isolated || IsLandmarkOrMega(contract);
	public static bool IsHighSkyCeiling(CandidateDeploymentSite site) => site.AltitudeBand == ResidenceAltitudeBand.HighSky && site.AssetArea.Y < HighSkyCeilingY;
	public static bool SurfaceContactOutOfRange(int surfaceDepth) => surfaceDepth < -MaximumSurfaceElevationAboveWorldSurface || surfaceDepth > MaximumSurfaceDepthBelowWorldSurface;
	public static bool SurfaceContactOutOfRange(AuthoredResidencePlacementContract contract, int surfaceDepth)
	{
		// Native contacts come from the sky-down mainland/support survey, not a
		// global Y guess. A real mountain can be >192 tiles above worldSurface.
		// Keep the depth/asset-height/relief checks; do not discard exposed hills.
		if (!Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive)
			return surfaceDepth > MaximumSurfaceDepthBelowWorldSurface;
		return contract.AnchorMode == ResidenceAnchorMode.CoastalWaterline
			? surfaceDepth < -MaximumCoastalWaterlineElevationAboveWorldSurface || surfaceDepth > MaximumSurfaceDepthBelowWorldSurface
			: SurfaceContactOutOfRange(surfaceDepth);
	}
	public static bool SurfaceAssetTooHigh(CandidateDeploymentSite site) =>
		site.AssetArea.Y < 1 || site.ProtectedArea.Top < WorldMargin;
	public static int VisualGap(AuthoredResidencePlacementContract left, AuthoredResidencePlacementContract right, int leftFocusWidth, int rightFocusWidth)
	{
		int baseGap = BaseGap(left.ScaleClass, right.ScaleClass);
		if (left.CanShareSettlement && right.CanShareSettlement && left.ClusterPolicy == right.ClusterPolicy && left.ClusterPolicy is ResidenceClusterPolicy.Hamlet or ResidenceClusterPolicy.Port)
		{
			baseGap = Math.Min(baseGap, 16);
		}

		bool leftHeavy = left.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega;
		bool rightHeavy = right.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega;
		if (leftHeavy && rightHeavy)
			baseGap = Math.Max(baseGap, 72);
		else if (left.RequiresIsolation && right.RequiresIsolation)
			baseGap = Math.Max(baseGap, 56);
		else if (left.RequiresIsolation || right.RequiresIsolation)
			baseGap = Math.Max(baseGap, 36);
		// MinimumVisualDistance is an isolation advertisement, not a pairwise
		// exclusion against every other residence. Snow/Jungle/Desert biomes on
		// Large FTW cannot host 240–280 tile gaps around 400–700 wide megas.
		if (leftHeavy && rightHeavy)
		{
			int advertised = Math.Min(left.MinimumVisualDistance, right.MinimumVisualDistance);
			baseGap = Math.Max(baseGap, Math.Min(advertised, 120));
		}

		int extra = (int)(0.12 * Math.Max(leftFocusWidth, rightFocusWidth));
		if (left.PreferredBiome == right.PreferredBiome && left.PreferredBiome is HomeBiome.Snow or HomeBiome.Desert or HomeBiome.Jungle)
		{
			baseGap = Math.Max(32, Math.Min(baseGap, 56));
			extra = Math.Min(extra, 36);
		}

		return baseGap + Math.Min(extra, 80);
	}
}
