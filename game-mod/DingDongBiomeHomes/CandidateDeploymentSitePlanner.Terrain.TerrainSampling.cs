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
	private static double SolidSampleRatio(Rectangle area)
	{
		int samples = 0;
		int solid = 0;
		for (int x = area.Left; x < area.Right; x += 8)
		{
			for (int y = area.Top; y < area.Bottom; y += 8)
			{
				samples++;
				if (WorldGen.SolidTile(x, y))
					solid++;
			}
		}

		return samples == 0 ? 0 : solid / (double)samples;
	}
}
