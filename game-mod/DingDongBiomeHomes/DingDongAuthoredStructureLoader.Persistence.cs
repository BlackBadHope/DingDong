using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace DingDongBiomeHomes;
/// <summary>
/// Runtime-independent reader for the reviewed Structure Helper 3.0.1/3.1.0
/// vanilla subset used by DingDong. It intentionally rejects NBT, custom
/// tile-data blocks and arbitrary modded tile/wall mappings instead of loading
/// another assembly.
/// </summary>
public static partial class DingDongAuthoredStructureLoader
{
	private static StructureWritePlan GetWritePlan(StructureData data, string resourcePath, AuthoredDeploymentProfile profile)
	{
		lock (cacheLock) return GetWritePlanLocked(data, resourcePath, profile);
	}

	private static StructureWritePlan GetWritePlanLocked(StructureData data, string resourcePath, AuthoredDeploymentProfile profile)
	{
		bool integrateNaturalBoundary = profile?.BoundaryPolicy == DeploymentBoundaryPolicy.PreserveBoundaryConnectedExterior;
		var plans = integrateNaturalBoundary ? IntegratedWritePlanCache : RawWritePlanCache;
		if (plans.TryGetValue(resourcePath, out StructureWritePlan cached))
			return cached;
		StructureWritePlan plan = BuildWritePlan(data, integrateNaturalBoundary);
		if (resourcePath == AuthoredSourceContext.JungleResource)
		{
			ValidateRestoredSourceContext(data, plan);
			if (integrateNaturalBoundary)
				plan = plan with { TerrainAnchorY = AuthoredSourceContext.JungleTerrainAnchorY };
		}
		plans.Add(resourcePath, plan);
		return plan;
	}
}
