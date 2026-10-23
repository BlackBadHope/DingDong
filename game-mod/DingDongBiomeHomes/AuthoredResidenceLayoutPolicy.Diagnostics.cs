using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes;

public static partial class AuthoredResidenceLayoutPolicy
{
	public static string FormatSiteLine(int index, CandidateDeploymentSite site, AuthoredPreset preset, IReadOnlyList<CandidateDeploymentSite> all)
	{
		AuthoredResidencePlacementContract contract = AuthoredResidencePlacementManifest.Required(site.NpcKey);
		Rectangle focus = site.AssetArea;
		int nearest = int.MaxValue;
		int local = 0;
		int sector = SectorOf(site.AssetArea.Center.X);
		int landmark = 0;
		foreach (CandidateDeploymentSite other in all)
		{
			if (string.Equals(other.NpcKey, site.NpcKey, StringComparison.Ordinal))
				continue;
			AuthoredResidencePlacementContract otherContract = AuthoredResidencePlacementManifest.Required(other.NpcKey);
			int gap = AxisGap(site.AssetArea, other.AssetArea);
			if (!(RequiresProtectedSeparation(contract) && !RequiresProtectedSeparation(otherContract)) && gap < nearest)
				nearest = gap;
			if (SectorOf(other.AssetArea.Center.X) == sector && otherContract.BroadLayer == contract.BroadLayer)
			{
				local++;
				if (otherContract.ScaleClass is ResidenceScaleClass.Landmark or ResidenceScaleClass.Mega)
					landmark++;
			}
		}

		int surfaceDepth = contract.BroadLayer == BroadLayoutLayer.Surface ? site.TargetReference.Y - (int)Main.worldSurface : -1;
		return $"DINGDONG_WORLDGEN_DISTRIBUTION|index={index}/61|npc={site.NpcKey}|preset={preset.Id}|kind={contract.Kind}|scale={contract.ScaleClass}|focus={focus.Width}x{focus.Height}|asset={preset.Width}x{preset.Height}|biome={site.Biome}|preferred={contract.PreferredBiome}|layer={contract.BroadLayer}|band={contract.PreferredAltitudeBand}|actualBand={site.AltitudeBand}|macroWindow={contract.MacroWindow}|sector={sector}|x={site.AssetArea.X}|y={site.AssetArea.Y}|targetY={site.TargetReference.Y}|surfaceDepth={surfaceDepth}|surfaceRelief={site.SurfaceRelief}|nnGap={(nearest == int.MaxValue ? -1 : nearest)}|localDensity={local}|landmarkDensity={landmark}|mass={contract.Mass}|cluster={contract.ClusterPolicy}";
	}
}
