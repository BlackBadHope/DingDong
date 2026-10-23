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
	private static bool HasOpenSkyCoastalWaterSurface(int x, int waterlineY, int scanTop)
	{
		int openWetColumns = 0;
		for (int sampleX = Math.Max(20, x - 12); sampleX <= Math.Min(Main.maxTilesX - 20, x + 12); sampleX += 4)
		{
			Tile water = Main.tile[sampleX, waterlineY];
			if (!IsOceanWater(water) || water.LiquidAmount < 32)
				continue;
			bool blockedFromSky = false;
			for (int sampleY = Math.Max(SurfaceScanTop, scanTop); sampleY < waterlineY; sampleY++)
			{
				Tile above = Main.tile[sampleX, sampleY];
				if (WorldGen.SolidTile(sampleX, sampleY) || above.WallType != WallID.None)
				{
					blockedFromSky = true;
					break;
				}
			}

			if (!blockedFromSky)
				openWetColumns++;
		}

		return openWetColumns >= 4;
	}
}
