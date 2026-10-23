using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalDungeonSurvey
{
	internal const int MaximumScanHalfWidth = 2400;
	internal const int MaximumScanDepth = 3600;
	private const int StyleSampleRows = 48;

	internal static int ScanHalfWidth => Math.Clamp(Main.maxTilesX / 16, 1000,
		MaximumScanHalfWidth);
	internal static int ScanDepth => Math.Clamp(Main.maxTilesY / 2, 1800,
		MaximumScanDepth);

	internal static ContinentalDungeonStyle DetectStyle()
	{
		int[] counts = new int[3];
		int left = Math.Max(30, Main.dungeonX - ScanHalfWidth);
		int right = Math.Min(Main.maxTilesX - 30, Main.dungeonX + ScanHalfWidth);
		int top = Math.Max(30, Main.dungeonY - 40);
		int bottom = Math.Min(NativeUnderworldSystem.Layer - 100, top + ScanDepth);
		int localLeft = Math.Max(left, Main.dungeonX - 180);
		int localRight = Math.Min(right, Main.dungeonX + 181);
		int localTop = Math.Max(top, Main.dungeonY - 40);
		int localBottom = Math.Min(bottom, Main.dungeonY + 260);
		for (int y = localTop; y < localBottom; y++)
			for (int x = localLeft; x < localRight; x++)
				CountBrick(Main.tile[x, y], counts);
		for (int sample = 0; sample < StyleSampleRows; sample++)
		{
			int y = top + (int)((long)sample * Math.Max(0, bottom - top - 1) /
				(StyleSampleRows - 1));
			for (int x = left; x < right; x++)
				CountBrick(Main.tile[x, y], counts);
		}
		int style = counts[1] > counts[0] ? 1 : 0;
		if (counts[2] > counts[style]) style = 2;
		if (counts[style] == 0)
			throw new InvalidOperationException("Bounded dungeon style samples found no vanilla brick.");
		return style switch
		{
			1 => new(TileID.GreenDungeonBrick, TileID.CrackedGreenDungeonBrick,
				WallID.GreenDungeonUnsafe),
			2 => new(TileID.PinkDungeonBrick, TileID.CrackedPinkDungeonBrick,
				WallID.PinkDungeonUnsafe),
			_ => new(TileID.BlueDungeonBrick, TileID.CrackedBlueDungeonBrick,
				WallID.BlueDungeonUnsafe)
		};
	}

	internal static bool TryFindEdges(int centerY, out ContinentalDungeonEdge leftEdge,
		out ContinentalDungeonEdge rightEdge)
	{
		leftEdge = ContinentalDungeonEdge.Missing;
		rightEdge = ContinentalDungeonEdge.Missing;
		int left = Math.Max(30, Main.dungeonX - ScanHalfWidth);
		int right = Math.Min(Main.maxTilesX - 30, Main.dungeonX + ScanHalfWidth);
		for (int y = Math.Max(30, centerY - 4);
			y <= Math.Min(Main.maxTilesY - 30, centerY + 4); y++)
		{
			int rowLeft = int.MaxValue;
			int rowRight = int.MinValue;
			for (int x = left; x <= right; x++)
			{
				Tile tile = Main.tile[x, y];
				if (!tile.HasTile || !ContinentalNaturalTiles.IsDungeonBrick(tile.TileType))
					continue;
				rowLeft = Math.Min(rowLeft, x);
				rowRight = Math.Max(rowRight, x);
			}
			if (rowLeft <= rowRight && OpensIntoDungeon(rowLeft, y, 1))
				SelectClosest(centerY, new(rowLeft, y), ref leftEdge);
			if (rowLeft <= rowRight && OpensIntoDungeon(rowRight, y, -1))
				SelectClosest(centerY, new(rowRight, y), ref rightEdge);
		}
		return leftEdge.IsValid || rightEdge.IsValid;
	}

	private static void CountBrick(Tile tile, int[] counts)
	{
		if (!tile.HasTile) return;
		if (tile.TileType is TileID.BlueDungeonBrick or TileID.CrackedBlueDungeonBrick) counts[0]++;
		else if (tile.TileType is TileID.GreenDungeonBrick or TileID.CrackedGreenDungeonBrick) counts[1]++;
		else if (tile.TileType is TileID.PinkDungeonBrick or TileID.CrackedPinkDungeonBrick) counts[2]++;
	}

	private static bool OpensIntoDungeon(int edgeX, int y, int inward)
	{
		for (int depth = 1; depth <= 4; depth++)
		{
			int x = edgeX + inward * depth;
			if (x <= 0 || x >= Main.maxTilesX - 1) return false;
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile && ContinentalNaturalTiles.IsDungeonWall(tile.WallType))
				return true;
		}
		return false;
	}

	private static void SelectClosest(int centerY, ContinentalDungeonEdge candidate,
		ref ContinentalDungeonEdge selected)
	{
		if (!selected.IsValid || Math.Abs(candidate.Y - centerY) < Math.Abs(selected.Y - centerY))
			selected = candidate;
	}
}
