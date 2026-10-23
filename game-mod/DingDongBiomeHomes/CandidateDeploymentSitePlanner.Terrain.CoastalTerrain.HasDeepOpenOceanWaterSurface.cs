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
	private static bool HasDeepOpenOceanWaterSurface(int x, int waterlineY)
	{
		int deepWetColumns = 0;
		int bottom = Math.Min(Main.maxTilesY - 20, waterlineY + 64);
		for (int sampleX = Math.Max(20, x - 12); sampleX <= Math.Min(Main.maxTilesX - 20, x + 12); sampleX += 4)
		{
			int samples = 0;
			int wetSamples = 0;
			for (int sampleY = waterlineY; sampleY <= bottom; sampleY += 4)
			{
				samples++;
				Tile water = Main.tile[sampleX, sampleY];
				if (IsOceanWater(water) && water.LiquidAmount >= 32)
					wetSamples++;
			}

			if (samples > 0 && wetSamples * 100 >= samples * 70)
				deepWetColumns++;
		}

		return deepWetColumns >= 4;
	}
}
