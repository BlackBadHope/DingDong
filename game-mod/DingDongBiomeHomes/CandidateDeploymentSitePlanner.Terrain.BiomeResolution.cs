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
	private static bool TryMatchAllowedBiome(int centerX, int centerY, AuthoredDeploymentProfile profile, AuthoredResidencePlacementContract contract, IReadOnlyList<CandidateDeploymentSite> plannedSites, out HomeBiome matchedBiome, out string fallback)
	{
		bool requireOceanWater = profile.TargetAnchor != DeploymentTargetAnchor.SurfaceTerrainEnvelope;
		bool allowWeatheredOpenCoast = profile.TargetAnchor == DeploymentTargetAnchor.CoastalWaterlineEnvelope || profile.TargetAnchor == DeploymentTargetAnchor.SurfaceTerrainEnvelope && contract.PreferredBiome == HomeBiome.Ocean;
		if (MatchesSurfaceBiome(centerX, centerY, contract.PreferredBiome, out fallback, requireOceanWater, allowWeatheredOpenCoast))
		{
			matchedBiome = contract.PreferredBiome;
			return true;
		}

		// AllowedBiomes is the reviewed deployment-compatibility contract. The
		// first allowed biome remains the authored deployment preference, but an
		// explicitly admitted secondary must not be silently disabled merely because
		// a residence is large or isolated (for example Nurse: Snow|Hallow).
		bool allowSecondary = contract.AllowedBiomes.Count > 1;
		if (allowSecondary)
		{
			foreach (HomeBiome biome in contract.AllowedBiomes)
			{
				if (biome == contract.PreferredBiome)
					continue;
				if (!MatchesSurfaceBiome(centerX, centerY, biome, out fallback, requireOceanWater, allowWeatheredOpenCoast))
					continue;
				matchedBiome = biome;
				fallback = $"CompatibleBiome:{biome}";
				return true;
			}
		}

		if (contract.CanShareSettlement && contract.ScaleClass is ResidenceScaleClass.Compact or ResidenceScaleClass.Medium && contract.ClusterPolicy is ResidenceClusterPolicy.Hamlet or ResidenceClusterPolicy.Port && MatchesAuthoredDistrictExpansion(centerX, centerY, contract.PreferredBiome, profile.TargetAnchor, plannedSites, out fallback))
		{
			matchedBiome = contract.PreferredBiome;
			return true;
		}

		matchedBiome = contract.PreferredBiome;
		fallback = string.Empty;
		return false;
	}

	private static bool MatchesAuthoredDistrictExpansion(int centerX, int centerY, HomeBiome biome, DeploymentTargetAnchor targetAnchor, IReadOnlyList<CandidateDeploymentSite> plannedSites, out string fallback)
	{
		fallback = string.Empty;
		if (biome is HomeBiome.Underground or HomeBiome.Mushroom or HomeBiome.Underworld || biome == HomeBiome.Ocean && targetAnchor != DeploymentTargetAnchor.SkyClearanceEnvelope)
			return false;
		int baseReach = 240;
		foreach (CandidateDeploymentSite existing in plannedSites)
		{
			if (existing.Biome != biome)
				continue;
			// Topographic surface anchors can differ sharply across FTW ridges and
			// valleys. Keep district expansion local in X while still allowing it to
			// follow the generated surface profile instead of the global layer line.
			if (existing.TargetReference.Y <= 0 || Math.Abs(centerY - existing.TargetReference.Y) > SurfaceDistrictVerticalReach)
				continue;
			int reach = baseReach + existing.AssetArea.Width / 2;
			if (Math.Abs(centerX - existing.TargetReference.X) > reach)
				continue;
			fallback = $"AuthoredDistrictExpansionFromNatural{biome}";
			return true;
		}

		return false;
	}
}
