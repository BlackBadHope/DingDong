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
	private static bool HasOpenSkyCoastalTerrain(Rectangle visualFocus)
	{
		int samples = 0;
		int openColumns = 0;
		int inset = Math.Min(8, Math.Max(2, visualFocus.Width / 8));
		int left = Math.Max(20, visualFocus.Left + inset);
		int right = Math.Min(Main.maxTilesX - 20, visualFocus.Right - inset - 1);
		int step = Math.Max(4, Math.Max(1, right - left) / 6);
		int scanBottom = Math.Min(Main.maxTilesY - 20, visualFocus.Top);
		for (int sampleX = left; sampleX <= right; sampleX += step)
		{
			samples++;
			bool blockedFromSky = false;
			for (int sampleY = SurfaceScanTop; sampleY < scanBottom; sampleY++)
			{
				Tile above = Main.tile[sampleX, sampleY];
				if (WorldGen.SolidTile(sampleX, sampleY) || above.WallType != WallID.None)
				{
					blockedFromSky = true;
					break;
				}
			}

			if (!blockedFromSky)
				openColumns++;
		}

		return samples >= 3 && openColumns * 100 >= samples * 60;
	}
}
