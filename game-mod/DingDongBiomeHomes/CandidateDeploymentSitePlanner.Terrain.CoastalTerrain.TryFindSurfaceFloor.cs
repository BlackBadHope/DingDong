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
	private static bool TryFindSurfaceFloor(int x, out int floorY)
	{
		int maxY = Math.Min(Main.maxTilesY - 120, (int)Main.worldSurface + AuthoredResidenceLayoutPolicy.MaximumSurfaceDepthBelowWorldSurface);
		return Worldgen.NativeSurfaceFloorSearch.TryFind(SurfaceScanTop, maxY,
			y => IsNaturalSurfaceFloor(Main.tile[x, y]),
			y => !WorldGen.SolidTile(x, y - 2) && !WorldGen.SolidTile(x, y - 6),
			y => HasDeepMainlandSupport(x, y), y => HasMainlandSupport(x, y), out floorY);
	}
}
