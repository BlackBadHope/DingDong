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
	private static bool TryEvaluatePlannedSiteInteractions(AuthoredResidencePlacementContract contract, CandidateDeploymentSite candidate, IReadOnlyList<CandidateDeploymentSite> planned, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, ref int score, ref string rejectReason, bool validateHardPairwise, int sector, BroadLayoutLayer layer, ref int count, ref int mass, ref int independent, ref int mega, ref int landmarksInCell, ref int landmarkMega, ref int nearest, ref int nearby, ref int skySameBand, ref int altitudeSameBand, ref int highSkyCeiling, ref int leftLayer, ref int rightLayer, ref int leftOcean, ref int rightOcean, List<int> layerXs, Rectangle focus)
	{
		foreach (CandidateDeploymentSite existing in planned)
		{
			AuthoredResidencePlacementContract other = AuthoredResidencePlacementManifest.Required(existing.NpcKey);
			if (validateHardPairwise && contract.ScaleClass == ResidenceScaleClass.Mega && other.ScaleClass == ResidenceScaleClass.Mega && contract.MacroWindow == ResidenceMacroWindow.HallowEitherShelf && other.MacroWindow == ResidenceMacroWindow.HallowEitherShelf && Math.Abs(candidate.AssetArea.Center.X - existing.AssetArea.Center.X) < HallowMegaShelfSeparation)
			{
				rejectReason = $"hallow-mega-shelf:{existing.NpcKey}";
				return false;
			}

			if (allMasks == null || !allMasks.TryGetValue(existing.NpcKey, out AuthoredSceneMutationMask existingMask))
			{
				rejectReason = $"missing-mask:{existing.NpcKey}";
				return false;
			}

			Rectangle existingFocus = existingMask.VisualFootprint(existing.AssetArea.Location, 0);
			int existingSector = SectorOf(existing.AssetArea.Center.X);
			BroadLayoutLayer existingLayer = other.BroadLayer;
			if (validateHardPairwise && !TryValidateOwnerPair(contract, candidate, focus, other, existing, existingFocus, out rejectReason))
				return false;
			AccumulatePlannedSiteMetrics(contract, candidate, ref score, sector, layer, ref count, ref mass, ref independent, ref mega, ref landmarksInCell, ref landmarkMega, ref altitudeSameBand, ref highSkyCeiling, ref leftLayer, ref rightLayer, ref leftOcean, ref rightOcean, layerXs, existing, other, existingSector, existingLayer);
			if (validateHardPairwise && HardVisualTooClose(contract, focus, other, existingFocus))
			{
				rejectReason = $"visual-spacing:{existing.NpcKey}";
				return false;
			}

			int required = VisualGap(contract, other, focus.Width, existingFocus.Width);
			if (TooClose(focus, existingFocus, required, layer, existingLayer))
				score -= 320;
			int gap = AxisGap(candidate.ProtectedArea, existing.ProtectedArea);
			if (gap < nearest)
				nearest = gap;
			if (existingLayer == layer && AxisGap(focus, existingFocus) < 48)
			{
				nearby++;
			}

			if (layer == BroadLayoutLayer.Sky && existingLayer == BroadLayoutLayer.Sky && Math.Abs(candidate.AssetArea.Y - existing.AssetArea.Y) < 40)
			{
				skySameBand++;
			}
		}

		return true;
	}
}
