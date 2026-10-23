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
	private static bool HasNearbyOpenCoastalWater(Rectangle visualFocus, SurfaceColumnIndex surfaceIndex)
	{
		int left = Math.Max(20, visualFocus.Left - CoastalTerrainEnvelopeMaximumOpenWaterGap);
		int right = Math.Min(Main.maxTilesX - 20, visualFocus.Right + CoastalTerrainEnvelopeMaximumOpenWaterGap);
		for (int sampleX = left; sampleX <= right; sampleX += 4)
		{
			int waterlineY;
			bool found = surfaceIndex != null ? surfaceIndex.TryWaterline(sampleX, out waterlineY) : TryFindCoastalWaterline(sampleX, out waterlineY);
			if (found)
				return true;
		}

		return false;
	}
}
