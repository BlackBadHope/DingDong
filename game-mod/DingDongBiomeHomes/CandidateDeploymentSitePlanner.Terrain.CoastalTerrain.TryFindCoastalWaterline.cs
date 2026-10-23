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
	private static bool TryFindCoastalWaterline(int x, out int waterlineY, bool allowCoastalSkyDomain = false)
	{
		// Floating ocean scenes already use the outer sixths in CoastalSkyRanges
		// and MatchesSurfaceBiomeSample. Grounded/waterline homes retain their
		// narrower coastal strip; no inland water becomes an eligible ocean.
		bool coastalSky = allowCoastalSkyDomain &&
			(x <= Main.maxTilesX / 6 || x >= Main.maxTilesX * 5 / 6);
		if (!IsInsideCoastalStrip(x) && !coastalSky)
		{
			waterlineY = 0;
			return false;
		}

		int minY = Math.Max(SurfaceScanTop, (int)Main.worldSurface - AuthoredResidenceLayoutPolicy.MaximumCoastalWaterlineElevationAboveWorldSurface);
		int maxY = Math.Min(Main.maxTilesY - 120, (int)Main.worldSurface + AuthoredResidenceLayoutPolicy.MaximumSurfaceDepthBelowWorldSurface);
		for (int y = minY; y < maxY; y++)
		{
			Tile tile = Main.tile[x, y];
			if (!IsOceanWater(tile) || tile.LiquidAmount < 64)
				continue;
			int wetColumns = 0;
			for (int sampleX = Math.Max(20, x - 12); sampleX <= Math.Min(Main.maxTilesX - 20, x + 12); sampleX += 4)
			{
				Tile sample = Main.tile[sampleX, y];
				if (IsOceanWater(sample) && sample.LiquidAmount >= 32)
					wetColumns++;
			}

			if (wetColumns < 4)
				continue;
			if (!HasOpenSkyCoastalWaterSurface(x, y, minY))
				continue;
			// A real ocean can be either a shallow sand shelf or deep water near the
			// world edge (Calamity's generated eastern sea is the important case).
			// Open sky remains mandatory above both forms, so this broadens the port
			// domain without re-admitting the covered cave pools rejected above.
			if (!HasWaterlineMainlandSupport(x, y) && !HasDeepOpenOceanWaterSurface(x, y))
				continue;
			waterlineY = y;
			return true;
		}

		waterlineY = 0;
		return false;
	}
}
