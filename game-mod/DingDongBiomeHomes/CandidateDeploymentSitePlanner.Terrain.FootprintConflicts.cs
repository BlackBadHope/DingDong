using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
public static partial class CandidateDeploymentSitePlanner
{
	private static bool InWorld(Rectangle area) => area.Left >= WorldMargin && area.Top >= WorldMargin && area.Right < Main.maxTilesX - WorldMargin && area.Bottom < Main.maxTilesY - WorldMargin;
	private static bool IntersectsReserved(Rectangle area, List<Rectangle> reserved)
	{
		foreach (Rectangle existing in reserved)
		{
			if (area.Intersects(existing))
				return true;
		}

		return false;
	}

	private static bool IntersectsPlannedMutations(Rectangle area, AuthoredSceneMutationMask mutationMask, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks)
	{
		if (allMasks == null)
			return true;
		foreach (CandidateDeploymentSite existing in plannedSites)
		{
			if (!area.Intersects(existing.AssetArea))
				continue;
			if (!allMasks.TryGetValue(existing.NpcKey, out AuthoredSceneMutationMask existingMask))
				return true;
			if (mutationMask.IntersectsTranslated(area.Location, existingMask, existing.AssetArea.Location))
				return true;
		}

		return false;
	}

	private static bool IntersectsPlannedVisualFocus(Rectangle area, AuthoredSceneMutationMask mutationMask, IReadOnlyList<CandidateDeploymentSite> plannedSites, IReadOnlyDictionary<string, AuthoredSceneMutationMask> allMasks, BroadLayoutLayer layer)
	{
		if (plannedSites.Count == 0)
			return false;
		if (allMasks == null)
			return true;
		Rectangle visualFocus = mutationMask.VisualFootprint(area.Location, VisualFocusClearance);
		foreach (CandidateDeploymentSite existing in plannedSites)
		{
			if (!allMasks.TryGetValue(existing.NpcKey, out AuthoredSceneMutationMask existingMask))
				return true;
			Rectangle existingFocus = existingMask.VisualFootprint(existing.AssetArea.Location, VisualFocusClearance);
			BroadLayoutLayer existingLayer = AuthoredResidencePlacementManifest.Required(existing.NpcKey).BroadLayer;
			if (layer != existingLayer && VerticallySeparated(visualFocus, existingFocus, 32))
				continue;
			if (visualFocus.Intersects(existingFocus))
				return true;
		}

		return false;
	}

	private static bool IntersectsPlannedVisualFocus(Rectangle area, AuthoredSceneMutationMask mutationMask, BroadLayoutLayer layer, CandidateDeploymentSite existing, AuthoredSceneMutationMask existingMask, BroadLayoutLayer existingLayer)
	{
		Rectangle visualFocus = mutationMask.VisualFootprint(area.Location, VisualFocusClearance);
		Rectangle existingFocus = existingMask.VisualFootprint(existing.AssetArea.Location, VisualFocusClearance);
		return (layer == existingLayer || !VerticallySeparated(visualFocus, existingFocus, 32)) && visualFocus.Intersects(existingFocus);
	}

	private static bool VerticallySeparated(Rectangle a, Rectangle b, int gap) => a.Bottom + gap <= b.Top || b.Bottom + gap <= a.Top;
}
