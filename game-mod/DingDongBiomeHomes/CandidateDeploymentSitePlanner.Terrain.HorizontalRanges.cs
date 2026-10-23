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
	private static List<(int Min, int Max)> CollectHorizontalRanges(AuthoredDeploymentProfile profile, AuthoredResidencePlacementContract contract, int width, SurfaceColumnIndex surfaceIndex)
	{
		List<(int Min, int Max)> ranges = new();
		HashSet<string> seen = new(StringComparer.Ordinal);
		foreach (HomeBiome biome in contract.AllowedBiomes)
		{
			// MacroWindow partitions the preferred Hallow shelves. Reviewed
			// purity fallbacks remain eligible through their own biome ranges.
			if (contract.MacroWindow != ResidenceMacroWindow.Any && biome != HomeBiome.Hallow && biome != HomeBiome.Forest)
			{
				continue;
			}

			List<(int Min, int Max)> biomeRanges;
			if (biome == HomeBiome.Ocean)
			{
				if (profile.TargetAnchor == DeploymentTargetAnchor.SkyClearanceEnvelope)
				{
					// A floating scene is ocean-authored by the biome under its center,
					// not by requiring its entire (potentially 550-tile) source envelope
					// to fit inside the narrow shoreline strip. Keep the exact ocean and
					// liquid gate below, but expose the complete coastal-center domain so
					// giant sky scenes can route around generated islands, planetoids and
					// persistent objects without weakening any collision protection.
					biomeRanges = CoastalSkyRanges(width);
				}
				else
				{
					int rightFloor = Main.maxTilesX - CoastalStripWidth;
					biomeRanges = CoastalRanges(width, includeRight: true, rightFloor);
				}
			}
			else if (surfaceIndex != null && contract.PreferredBiome == HomeBiome.Snow && biome == HomeBiome.Forest)
			{
				biomeRanges = surfaceIndex.StartSnowEdge(width);
				// Snow proximity is a preference, not a stricter biome contract.
				// Keep edge candidates first, but include the explicitly allowed
				// forest domain when the entire snow edge has unsuitable relief.
				biomeRanges.AddRange(surfaceIndex.StartRanges(HomeBiome.Forest, width));
			}
			else if (surfaceIndex != null && biome == HomeBiome.Hallow)
			{
				biomeRanges = surfaceIndex.StartHallowRanges(width, contract.MacroWindow);
			}
			else if (surfaceIndex != null)
			{
				biomeRanges = surfaceIndex.StartRanges(biome, width);
			}
			else
			{
				biomeRanges = SurfaceRanges(biome, width);
			}

			foreach ((int min, int max)in biomeRanges)
			{
				string key = $"{min}:{max}";
				if (seen.Add(key))
					ranges.Add((min, max));
			}
		}

		if (ranges.Count == 0 && contract.MacroWindow == ResidenceMacroWindow.Any)
			ranges.AddRange(SurfaceRanges(profile.TargetBiome, width));
		return ranges;
	}

	private static List<(int Min, int Max)> SurfaceRanges(HomeBiome biome, int width)
	{
		if (biome == HomeBiome.Ocean)
			return CoastalRanges(width, includeRight: true,
				rightFloor: Main.maxTilesX - CoastalStripWidth);
		return new List<(int, int)>
		{
			(180, Main.maxTilesX - 180 - width)
		};
	}

	private static List<(int Min, int Max)> CoastalRanges(int width, bool includeRight, int rightFloor)
	{
		int leftMax = Math.Min(CoastalStripWidth - width - 12,
			Main.maxTilesX / 3 - width);
		List<(int, int)> ranges = new();
		if (leftMax >= 90)
			ranges.Add((90, leftMax));
		if (!includeRight)
			return ranges;
		int rightMin = Math.Max(rightFloor + 12,
			Main.maxTilesX - CoastalStripWidth);
		int rightMax = Main.maxTilesX - 90 - width;
		if (rightMax >= rightMin)
			ranges.Add((rightMin, rightMax));
		return ranges;
	}

	private static List<(int Min, int Max)> CoastalSkyRanges(int width)
	{
		int halfWidth = width / 2;
		int minLeft = 90;
		int maxLeft = Main.maxTilesX - 90 - width;
		int leftMax = Math.Min(maxLeft, Main.maxTilesX / 6 - halfWidth);
		int rightMin = Math.Max(minLeft, Main.maxTilesX * 5 / 6 - halfWidth);
		List<(int, int)> ranges = new();
		if (leftMax >= minLeft)
			ranges.Add((minLeft, leftMax));
		if (maxLeft >= rightMin)
			ranges.Add((rightMin, maxLeft));
		return ranges;
	}
}
