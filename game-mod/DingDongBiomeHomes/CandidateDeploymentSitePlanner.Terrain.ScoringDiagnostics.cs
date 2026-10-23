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
	private static bool Fail(string reason, out CandidateDeploymentSite site, out string failure)
	{
		site = null;
		failure = reason;
		return false;
	}

	private static int PositiveHash(string value)
	{
		unchecked
		{
			int hash = 17;
			foreach (char character in value ?? string.Empty)
				hash = hash * 31 + character;
			return hash & int.MaxValue;
		}
	}

	private static int BiomeEdgeBonus(AuthoredResidencePlacementContract contract, int left, int rangeMin, int rangeMax)
	{
		int leftover = Math.Max(left - rangeMin, rangeMax - left);
		bool tightBiome = contract.PreferredBiome is HomeBiome.Snow or HomeBiome.Desert or HomeBiome.Jungle;
		if (tightBiome && contract.ScaleClass is ResidenceScaleClass.Mega or ResidenceScaleClass.Landmark)
			return leftover;
		if (tightBiome)
			return leftover / 2;
		if (contract.ScaleClass is ResidenceScaleClass.Mega or ResidenceScaleClass.Landmark)
			return Math.Min(leftover / 6, 200);
		return Math.Min(leftover / 8, 80);
	}

	private static void CountScoreReject(string reason, ref int spacingRejected, ref int independentRejected, ref int megaRejected, ref int densityRejected, ref int massRejected, ref int landmarkRejected)
	{
		if (reason.StartsWith("independent-density", StringComparison.Ordinal))
			independentRejected++;
		else if (reason.StartsWith("mega-sector", StringComparison.Ordinal) || reason.StartsWith("landmark-mega", StringComparison.Ordinal))
			megaRejected++;
		else if (reason.StartsWith("local-density", StringComparison.Ordinal) || reason.StartsWith("site-density", StringComparison.Ordinal))
			densityRejected++;
		else if (reason.StartsWith("sector-mass", StringComparison.Ordinal))
			massRejected++;
		else if (reason.StartsWith("landmark-adjacent", StringComparison.Ordinal))
			landmarkRejected++;
		else
			spacingRejected++;
	}
}
