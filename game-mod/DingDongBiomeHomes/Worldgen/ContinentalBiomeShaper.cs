using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalBiomeShaper
{
	private const int ColumnBatch = 12;

	internal static ContinentalBiomeShapeReceipt Shape(ContinentalSurfaceSurvey survey)
	{
		bool[] allowed = ContinentalPlacementMaskPolicy.CreateStructureOnly();
		var receipt = new ContinentalBiomeShapeReceipt();
		for (int x = Main.maxTilesX / 30; x < Main.maxTilesX - Main.maxTilesX / 30;)
		{
			ContinentalSurfaceKind owner = survey.OwnerAt(x);
			int firstGround = survey.GroundAt(x);
			ContinentalBiomeKindMetrics metrics = receipt.For(owner);
			if (metrics == null || firstGround < 0)
			{
				x++;
				continue;
			}
			int end = x + 1;
			while (end < Main.maxTilesX && end - x < ColumnBatch &&
				survey.OwnerAt(end) == owner && survey.GroundAt(end) >= 0 &&
				Math.Abs(survey.GroundAt(end) - firstGround) <= 32)
				end++;
			int columns = end - x;
			metrics.Own(columns);
			int depth = ContinentalBiomeVolumePolicy.Depth(owner);
			Rectangle area = BatchArea(survey, x, end, depth);
			if (GenVars.structures.CanPlace(area, allowed, 2))
			{
				metrics.Cover(columns);
				int frameTop = int.MaxValue;
				int frameBottom = int.MinValue;
				for (int column = x; column < end; column++)
					ShapeColumn(column, survey.GroundAt(column), owner, depth, metrics,
						ref frameTop, ref frameBottom);
				if (frameTop <= frameBottom)
					WorldGen.RangeFrame(x, frameTop, end - 1, frameBottom);
			}
			else metrics.SkipProtected(columns);
			x = end;
		}
		return receipt;
	}

	private static Rectangle BatchArea(ContinentalSurfaceSurvey survey, int left,
		int right, int depth)
	{
		int top = int.MaxValue;
		int bottom = 0;
		for (int x = left; x < right; x++)
		{
			int y = survey.GroundAt(x);
			top = Math.Min(top, y);
			bottom = Math.Max(bottom, y + depth);
		}
		return new(left, top, right - left,
			Math.Min(Main.maxTilesY - 20, bottom) - top);
	}

	private static void ShapeColumn(int x, int ground, ContinentalSurfaceKind owner,
		int depth, ContinentalBiomeKindMetrics metrics, ref int frameTop,
		ref int frameBottom)
	{
		int bottom = Math.Min(Main.maxTilesY - 20, ground + depth);
		for (int y = ground; y < bottom; y++)
		{
			Tile tile = Main.tile[x, y];
			if (!tile.HasTile || !ContinentalNaturalTiles.CanShape(tile.TileType))
				continue;
			ushort next = Replacement(owner, y - ground, depth);
			bool correct = tile.TileType == next;
			metrics.Observe(correct);
			if (correct) continue;
			tile.ClearTile();
			tile.HasTile = true;
			tile.TileType = next;
			frameTop = Math.Min(frameTop, y);
			frameBottom = Math.Max(frameBottom, y);
		}
	}

	private static ushort Replacement(ContinentalSurfaceKind kind, int offset,
		int totalDepth) => kind switch
	{
		ContinentalSurfaceKind.Desert when offset * 5 < totalDepth => TileID.Sand,
		ContinentalSurfaceKind.Desert when offset * 16 < totalDepth * 9 =>
			TileID.HardenedSand,
		ContinentalSurfaceKind.Desert => TileID.Sandstone,
		ContinentalSurfaceKind.Snow when offset * 14 < totalDepth * 5 =>
			TileID.SnowBlock,
		ContinentalSurfaceKind.Snow => TileID.IceBlock,
		ContinentalSurfaceKind.Jungle when offset == 0 => TileID.JungleGrass,
		_ => TileID.Mud
	};
}
