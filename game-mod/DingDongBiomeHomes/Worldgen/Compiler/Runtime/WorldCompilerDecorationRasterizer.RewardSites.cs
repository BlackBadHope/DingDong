using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerDecorationRasterizer
{
	private const int RewardHorizontalAttempts = 65;
	private const int RewardVerticalBands = 17;
	private const int RewardSiteStep = 8;
	private const int RewardVerticalStep = 32;

	private static bool TryPlaceChest(WorldCompilerPlan plan,
		WorldCompilerDecoration entry, HashSet<long> chestTiles, WorldCompilerSpatialIndex protectedAreas,
		out int chestIndex, out bool relocated)
	{
		int style = entry.Kind == WorldCompilerDecorationKind.SurfaceChest ? 0 : 1;
		int attempts = RewardSiteAttempts(entry);
		for (int attempt = 0; attempt < attempts; attempt++)
		{
			(int x, int y) = RewardSite(plan, entry, attempt);
			if (!CanPrepareSite(plan, x, y, 2, chestTiles, protectedAreas)) continue;
			PrepareSite(x, y, 2);
			chestIndex = WorldGen.PlaceChest(x, y, TileID.Containers, false, style);
			if (chestIndex < 0) continue;
			MarkChestTiles(chestTiles, x, y);
			relocated = attempt != 0;
			return true;
		}
		chestIndex = -1;
		relocated = false;
		return false;
	}

	private static bool TryPlaceLifeCrystal(WorldCompilerPlan plan,
		WorldCompilerDecoration entry, HashSet<long> chestTiles, WorldCompilerSpatialIndex protectedAreas,
		out bool relocated)
	{
		int attempts = RewardSiteAttempts(entry);
		for (int attempt = 0; attempt < attempts; attempt++)
		{
			(int x, int y) = RewardSite(plan, entry, attempt);
			if (!CanPrepareSite(plan, x, y, 2, chestTiles, protectedAreas)) continue;
			PrepareSite(x, y, 2);
			if (!WorldGen.PlaceTile(x, y, TileID.Heart, true, true)) continue;
			relocated = attempt != 0;
			return true;
		}
		relocated = false;
		return false;
	}

	private static (int X, int Y) RewardSite(WorldCompilerPlan plan,
		WorldCompilerDecoration entry, int attempt)
	{
		int horizontal = attempt % RewardHorizontalAttempts;
		int vertical = attempt / RewardHorizontalAttempts;
		int distance = horizontal == 0 ? 0 :
			(horizontal + 1) / 2 * RewardSiteStep;
		int offset = horizontal == 0 ? 0 :
			(horizontal & 1) == 1 ? -distance : distance;
		int x = entry.X + offset;
		int y = entry.Kind == WorldCompilerDecorationKind.SurfaceChest &&
			x >= 0 && x < plan.Width ? plan.Surface[x] - 1 :
			entry.Y + SignedBandOffset(vertical, RewardVerticalStep);
		return (x, y);
	}

	private static int RewardSiteAttempts(WorldCompilerDecoration entry) =>
		entry.Kind == WorldCompilerDecorationKind.SurfaceChest ?
		RewardHorizontalAttempts : RewardHorizontalAttempts * RewardVerticalBands;

	private static int SignedBandOffset(int band, int step)
	{
		if (band == 0) return 0;
		int distance = (band + 1) / 2 * step;
		return (band & 1) == 1 ? -distance : distance;
	}

	private static bool CanPrepareSite(WorldCompilerPlan plan, int x, int bottomY,
		int width, HashSet<long> chestTiles, WorldCompilerSpatialIndex protectedAreas)
	{
		if (x < 3 || x + width + 2 >= plan.Width || bottomY < 4 ||
			bottomY + 3 >= plan.Height) return false;
		if (protectedAreas.Overlaps(new WorldCompilerRect(x, bottomY - 3, width, 6)))
			return false;
		for (int scanX = x; scanX < x + width; scanX++)
		for (int scanY = bottomY - 3; scanY <= bottomY + 2; scanY++)
		{
			if (chestTiles.Contains(TileKey(scanX, scanY))) return false;
			Tile tile = Main.tile[scanX, scanY];
			if (tile.HasTile && (tile.TileType >= TileID.Count ||
				Main.tileFrameImportant[tile.TileType])) return false;
		}
		return true;
	}

	private static HashSet<long> CaptureChestTiles()
	{
		var occupied = new HashSet<long>();
		foreach (Chest chest in Main.chest)
			if (chest != null) MarkChestTiles(occupied, chest.x, chest.y);
		return occupied;
	}

	private static void MarkChestTiles(HashSet<long> occupied, int x, int y)
	{
		occupied.Add(TileKey(x, y));
		occupied.Add(TileKey(x + 1, y));
		occupied.Add(TileKey(x, y + 1));
		occupied.Add(TileKey(x + 1, y + 1));
	}

	private static long TileKey(int x, int y) => ((long)x << 32) | (uint)y;

	private static void PrepareSite(int x, int bottomY, int width)
	{
		for (int dx = 0; dx < width; dx++)
		{
			for (int y = bottomY - 2; y <= bottomY; y++)
			{
				Tile tile = Main.tile[x + dx, y];
				tile.ClearTile();
				tile.LiquidAmount = 0;
			}
			Tile floor = Main.tile[x + dx, bottomY + 1];
			floor.ClearTile();
			floor.LiquidAmount = 0;
			floor.HasTile = true;
			floor.TileType = TileID.Stone;
		}
	}

	private static InvalidOperationException RewardFailure(string kind,
		WorldCompilerDecoration entry)
	{
		int used = 0;
		foreach (Chest chest in Main.chest) if (chest != null) used++;
		return new InvalidOperationException($"Planned {kind} failed after " +
			$"{RewardSiteAttempts(entry)} sites: origin={entry.X},{entry.Y};" +
			$"chests={used}/{Main.chest.Length}.");
	}
}
