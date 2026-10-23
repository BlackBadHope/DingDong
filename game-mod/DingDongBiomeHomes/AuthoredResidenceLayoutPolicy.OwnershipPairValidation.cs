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
	private static bool TryValidateOwnerPair(AuthoredResidencePlacementContract leftContract, CandidateDeploymentSite leftSite, Rectangle leftFocus, AuthoredResidencePlacementContract rightContract, CandidateDeploymentSite rightSite, Rectangle rightFocus, out string rejectReason)
	{
		if (IsLandmarkOrMega(leftContract) && IsLandmarkOrMega(rightContract) && leftSite.Biome == rightSite.Biome && SectorOf(leftSite.AssetArea.Center.X) == SectorOf(rightSite.AssetArea.Center.X))
		{
			if (leftContract.BroadLayer == rightContract.BroadLayer)
			{
				rejectReason = "landmark-mega-sector";
				return false;
			}

			if (leftContract.BroadLayer is BroadLayoutLayer.Sky or BroadLayoutLayer.Surface && rightContract.BroadLayer is BroadLayoutLayer.Sky or BroadLayoutLayer.Surface && VerticallyOverlaps(leftFocus, rightFocus))
			{
				rejectReason = "landmark-mega-stack";
				return false;
			}
		}

		// Check landmark/mega ownership before classifying a bounding-box-only
		// isolated overlap. The safety fallback may soften only the latter when
		// the exact authored mutation masks remain disjoint.
		if (RequiresProtectedSeparation(leftContract) && RequiresProtectedSeparation(rightContract) && AxisGap(leftFocus, rightFocus) < MinimumIsolatedProtectedGap)
		{
			rejectReason = "isolated-envelope";
			return false;
		}

		rejectReason = string.Empty;
		return true;
	}
}
