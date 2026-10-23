using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalAmazonTreeBuilder
{
	internal static void Build(Rectangle area, int centerX, int groundY, int height)
	{
		int[] centers = BuildTrunk(centerX, groundY, height);
		BuildRoots(centerX, groundY, Math.Clamp(height / 30, 5, 9));
		BuildBranches(centers, groundY, height);
		int topStep = height - 1;
		PlaceCanopy(centers[topStep], groundY - topStep + 8,
			Math.Clamp(height / 5, 30, 48), Math.Clamp(height / 10, 15, 24));
		Frame(area);
	}

	private static int[] BuildTrunk(int centerX, int groundY, int height)
	{
		var centers = new int[height];
		int drift = 0;
		int baseRadius = Math.Clamp(height / 30, 5, 9);
		for (int step = 0; step < height; step++)
		{
			if (step > 0 && step % 24 == 0)
				drift = Math.Clamp(drift + WorldGen.genRand.Next(-1, 2), -5, 5);
			centers[step] = centerX + drift;
			int radius = Math.Max(3, baseRadius - step * (baseRadius - 3) / height);
			PlaceDisk(centers[step], groundY - step, radius, TileID.LivingMahogany);
		}
		return centers;
	}

	private static void BuildRoots(int centerX, int groundY, int baseRadius)
	{
		for (int side = -1; side <= 1; side += 2)
		{
			for (int root = 0; root < 3; root++)
			{
				int length = WorldGen.genRand.Next(22, 38);
				for (int step = 0; step < length; step++)
				{
					int x = centerX + side * (baseRadius + step) + side * root * 2;
					int y = groundY + step / 3 + root;
					PlaceDisk(x, y, Math.Max(1, 3 - step * 3 / length), TileID.LivingMahogany);
				}
			}
		}
	}

	private static void BuildBranches(int[] centers, int groundY, int height)
	{
		int[] levels = { height / 3, height * 2 / 3, height * 4 / 5 };
		foreach (int level in levels)
		{
			for (int side = -1; side <= 1; side += 2)
			{
				int length = WorldGen.genRand.Next(25, 46);
				int rise = WorldGen.genRand.Next(8, 18);
				for (int step = 0; step < length; step++)
				{
					int x = centers[level] + side * step;
					int y = groundY - level - step * rise / length;
					PlaceDisk(x, y, Math.Max(1, 4 - step * 4 / length), TileID.LivingMahogany);
				}
				int endX = centers[level] + side * length;
				int endY = groundY - level - rise;
				PlaceCanopy(endX, endY, WorldGen.genRand.Next(16, 25), WorldGen.genRand.Next(10, 16));
			}
		}
	}

	private static void PlaceCanopy(int centerX, int centerY, int radiusX, int radiusY)
	{
		for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
		{
			for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
			{
				double dx = (x - centerX) / (double)radiusX;
				double dy = (y - centerY) / (double)radiusY;
				if (dx * dx + dy * dy > 1.0 || WorldGen.genRand.Next(13) == 0)
					continue;
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType == TileID.LivingMahogany)
					continue;
				tile.ClearTile();
				tile.HasTile = true;
				tile.TileType = TileID.LivingMahoganyLeaves;
				tile.LiquidAmount = 0;
			}
		}
	}

	private static void PlaceDisk(int centerX, int centerY, int radius, ushort type)
	{
		for (int x = centerX - radius; x <= centerX + radius; x++)
			for (int y = centerY - radius; y <= centerY + radius; y++)
				if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radius * radius)
				{
					Tile tile = Main.tile[x, y];
					tile.ClearTile();
					tile.HasTile = true;
					tile.TileType = type;
					tile.LiquidAmount = 0;
				}
	}

	private static void Frame(Rectangle area)
	{
		for (int x = area.Left; x < area.Right; x++)
			for (int y = area.Top; y < area.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && tile.TileType is TileID.LivingMahogany or TileID.LivingMahoganyLeaves)
					WorldGen.SquareTileFrame(x, y, true);
			}
	}
}
