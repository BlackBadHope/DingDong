using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class CandidateDeploymentSitePlanner
{
	private sealed partial class SurfacePlanningSearch
	{
		internal bool TryFindEdgeContacts(Worldgen.BoundedRefinementBudget budget, out CandidateDeploymentSite site, out string failure)
		{
			if (!spreadLayout || ActiveRankedCollector == null || surfaceIndex == null ||
				Worldgen.Compiler.Runtime.WorldCompilerActivation.IsActive ||
				profile.TargetAnchor != DeploymentTargetAnchor.SurfaceTerrainEnvelope)
				return Fail("edge-refinement-ineligible", out site, out failure);
			ScanEdgeAnchorRanges(budget);
			return FinishSearch(out site, out failure);
		}

		private void ScanEdgeAnchorRanges(Worldgen.BoundedRefinementBudget budget = null)
		{
			Rectangle footprint = AuthoredSourceContext.ReferenceContactBounds(preset,
				mutationMask.VisualFootprint(Point.Zero, 0));
			foreach (int contactOffset in Worldgen.SurfaceAnchorRangeProjection.ContactOffsets(
				preset.Width, footprint.Left, footprint.Width))
			{
				// Indexed land ranges describe a matching column at sourceWidth/2.
				// Moving the contact to a footprint quarter must translate the left
				// interval too; reusing it omitted valid biome-edge placements.
				// Ocean terrain ranges instead constrain the entire coastal scene,
				// so keep those original full-envelope limits unchanged.
				var contactRanges = profile.TargetBiome == HomeBiome.Ocean ? ranges :
					Worldgen.SurfaceAnchorRangeProjection.Translate(indexedCenterRanges, preset.Width,
						contactOffset, 180, Main.maxTilesX - 180 - preset.Width);
				foreach ((int min, int max) in contactRanges)
				{
					if (budget != null && budget.Remaining == 0) return;
					TryScanRange(min, max, out _, edgeAnchorOffset: contactOffset, edgeBudget: budget);
					TryScanRange(min, max, out _, refine: true, edgeAnchorOffset: contactOffset, edgeBudget: budget);
				}
			}
		}
	}
}
