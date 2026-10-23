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
	private static bool LooksLikeOrganicCavern(Rectangle area, bool mushroomRequired)
	{
		int samples = 0;
		int solid = 0;
		int open = 0;
		int naturalWall = 0;
		int mushroom = 0;
		int evil = 0;
		for (int x = area.Left; x < area.Right; x += 8)
		{
			for (int y = area.Top; y < area.Bottom; y += 8)
			{
				samples++;
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && Main.tileSolid[tile.TileType])
					solid++;
				else
					open++;
				if (tile.WallType != WallID.None && (tile.WallType >= WallID.Count || !Main.wallHouse[tile.WallType]))
					naturalWall++;
				if (tile.HasTile && tile.TileType == TileID.MushroomGrass)
					mushroom++;
				if (tile.HasTile && IsEvil(tile.TileType))
					evil++;
			}
		}

		if (samples == 0 || solid < samples / 10 || open < samples / 20 || evil > samples / 8)
			return false;
		if (mushroomRequired)
			return mushroom >= 2;
		return naturalWall >= samples / 12;
	}

	private static bool HasReviewedCavernGroundSupport(Rectangle area, int sourceGroundOffset)
	{
		int groundY = area.Bottom - 1 - sourceGroundOffset;
		if (groundY < 20 || groundY >= Main.maxTilesY - 20)
			return false;
		int inset = Math.Min(3, Math.Max(0, area.Width / 4));
		int left = area.Left + inset;
		int right = area.Right - inset;
		int step = Math.Max(2, Math.Max(1, right - left) / 8);
		int supportBottom = Math.Min(Main.maxTilesY - 20, groundY + Math.Max(4, sourceGroundOffset));
		int samples = 0;
		int supported = 0;
		for (int x = left; x < right; x += step)
		{
			samples++;
			for (int y = groundY; y <= supportBottom; y++)
			{
				if (!WorldGen.SolidTile(x, y))
					continue;
				supported++;
				break;
			}
		}

		return samples >= 3 && supported * 100 >= samples * 40;
	}

	private static bool TryAlignReviewedCavernGroundTop(int left, int proposedTop, AuthoredPreset preset, int sourceGroundOffset, int minimumTop, int maximumTop, out int alignedTop)
	{
		const int searchRadius = 24;
		for (int distance = 0; distance <= searchRadius; distance++)
		{
			int lowerTop = proposedTop + distance;
			if (lowerTop <= maximumTop && HasReviewedCavernGroundSupport(new Rectangle(left, lowerTop, preset.Width, preset.Height), sourceGroundOffset))
			{
				alignedTop = lowerTop;
				return true;
			}

			if (distance == 0)
				continue;
			int upperTop = proposedTop - distance;
			if (upperTop >= minimumTop && HasReviewedCavernGroundSupport(new Rectangle(left, upperTop, preset.Width, preset.Height), sourceGroundOffset))
			{
				alignedTop = upperTop;
				return true;
			}
		}

		alignedTop = proposedTop;
		return false;
	}

	private static bool LooksLikeUnderworldShelf(Rectangle area)
	{
		int samples = 0;
		int underworld = 0;
		int lava = 0;
		for (int x = area.Left; x < area.Right; x += 8)
		{
			for (int y = area.Top; y < area.Bottom; y += 8)
			{
				samples++;
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && MatchesBiomeTile(tile.TileType, HomeBiome.Underworld))
					underworld++;
				if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava)
					lava++;
			}
		}

		return samples > 0 && underworld >= samples / 12 && lava >= 1;
	}
}
