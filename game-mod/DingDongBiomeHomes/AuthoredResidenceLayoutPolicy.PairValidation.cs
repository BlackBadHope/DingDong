using System;
using Microsoft.Xna.Framework;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
	public static bool TryValidateHardPair(AuthoredResidencePlacementContract leftContract, AuthoredSceneMutationMask leftMask, CandidateDeploymentSite leftSite, AuthoredResidencePlacementContract rightContract, AuthoredSceneMutationMask rightMask, CandidateDeploymentSite rightSite, out string rejectReason)
	{
		Rectangle leftFocus = leftMask.VisualFootprint(leftSite.AssetArea.Location, 0);
		Rectangle rightFocus = rightMask.VisualFootprint(rightSite.AssetArea.Location, 0);
		bool sameHallowMegaShelf = leftContract.ScaleClass == ResidenceScaleClass.Mega && rightContract.ScaleClass == ResidenceScaleClass.Mega && leftContract.MacroWindow == ResidenceMacroWindow.HallowEitherShelf && rightContract.MacroWindow == ResidenceMacroWindow.HallowEitherShelf && Math.Abs(leftSite.AssetArea.Center.X - rightSite.AssetArea.Center.X) < HallowMegaShelfSeparation;
		if (sameHallowMegaShelf)
		{
			rejectReason = "hallow-mega-shelf";
			return false;
		}

		if (!TryValidateOwnerPair(leftContract, leftSite, leftFocus, rightContract, rightSite, rightFocus, out rejectReason))
			return false;
		if (HardVisualTooClose(leftContract, leftFocus, rightContract, rightFocus))
		{
			rejectReason = "visual-spacing";
			return false;
		}

		rejectReason = string.Empty;
		return true;
	}
}
