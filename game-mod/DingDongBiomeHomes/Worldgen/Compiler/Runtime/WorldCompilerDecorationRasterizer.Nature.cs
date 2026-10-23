using System;
using Terraria;
using Terraria.ID;

namespace DingDongBiomeHomes.Worldgen.Compiler.Runtime;

internal static partial class WorldCompilerDecorationRasterizer
{
	private static long RasterNatural(WorldCompilerPlan plan,
		WorldCompilerDecoration entry) => entry.Kind switch
	{
		WorldCompilerDecorationKind.Cactus => Cactus(plan, entry),
		WorldCompilerDecorationKind.MushroomCavern => MushroomCavern(plan, entry),
		WorldCompilerDecorationKind.AmazonTitanTree => TitanTree(plan, entry),
		WorldCompilerDecorationKind.AmazonTree => Tree(plan, entry,
			TileID.LivingMahogany, TileID.LivingMahoganyLeaves),
		WorldCompilerDecorationKind.ArcticPine => Pine(plan, entry),
		_ => Tree(plan, entry, TileID.LivingWood, TileID.LeafBlock)
	};

	private static long Tree(WorldCompilerPlan plan, WorldCompilerDecoration entry,
		ushort wood, ushort leaves)
	{
		long changed = 0;
		int top = Math.Max(2, entry.Y - entry.Height + 3);
		for (int y = top; y <= entry.Y; y++) changed += Set(plan, entry.X, y, wood);
		changed += Disk(plan, entry.X, top, Math.Max(4, entry.Width / 2),
			Math.Max(3, entry.Width / 3), leaves, wood);
		changed += Disk(plan, entry.X - 3, top + 3, 4, 3, leaves, wood);
		changed += Disk(plan, entry.X + 3, top + 3, 4, 3, leaves, wood);
		return changed;
	}

	private static long Pine(WorldCompilerPlan plan, WorldCompilerDecoration entry)
	{
		long changed = 0;
		int top = Math.Max(2, entry.Y - entry.Height + 2);
		for (int y = top; y <= entry.Y; y++)
		{
			changed += Set(plan, entry.X, y, TileID.WoodBlock);
			int radius = Math.Min(6, Math.Max(0, (y - top) / 2));
			for (int x = entry.X - radius; x <= entry.X + radius; x++)
				if (x != entry.X) changed += Set(plan, x, y,
					TileID.LivingMahoganyLeaves);
		}
		return changed;
	}

	private static long Cactus(WorldCompilerPlan plan,
		WorldCompilerDecoration entry)
	{
		long changed = 0;
		for (int step = 0; step < entry.Height; step++)
			changed += Set(plan, entry.X, entry.Y - step, TileID.Cactus);
		if (entry.Height >= 6)
		{
			changed += Set(plan, entry.X - 1, entry.Y - 3, TileID.Cactus);
			changed += Set(plan, entry.X + 1, entry.Y - 4, TileID.Cactus);
		}
		return changed;
	}

	private static long TitanTree(WorldCompilerPlan plan,
		WorldCompilerDecoration entry)
	{
		long changed = 0;
		int top = Math.Max(30, entry.Y - entry.Height);
		int trunkRadius = Math.Clamp(entry.Height / 34, 5, 9);
		for (int y = top; y <= entry.Y; y++)
		{
			int radius = Math.Max(3, trunkRadius -
				(entry.Y - y) * (trunkRadius - 3) / entry.Height);
			changed += Disk(plan, entry.X, y, radius, radius,
				TileID.LivingMahogany, ushort.MaxValue);
		}
		for (int level = 1; level <= 3; level++)
		{
			int y = entry.Y - entry.Height * level / 4;
			int reach = 22 + level * 6;
			changed += Branch(plan, entry.X, y, -reach);
			changed += Branch(plan, entry.X, y - 5, reach);
		}
		changed += Disk(plan, entry.X, top + 8, 42, 19,
			TileID.LivingMahoganyLeaves, TileID.LivingMahogany);
		return changed;
	}

	private static long Branch(WorldCompilerPlan plan, int x, int y, int reach)
	{
		long changed = 0;
		int direction = Math.Sign(reach);
		for (int step = 0; step <= Math.Abs(reach); step++)
			changed += Disk(plan, x + direction * step, y - step / 4,
				Math.Max(1, 4 - step * 4 / Math.Abs(reach)), 2,
				TileID.LivingMahogany, ushort.MaxValue);
		int endX = x + reach;
		changed += Disk(plan, endX, y - Math.Abs(reach) / 4, 16, 10,
			TileID.LivingMahoganyLeaves, TileID.LivingMahogany);
		return changed;
	}

	private static long Disk(WorldCompilerPlan plan, int centerX, int centerY,
		int radiusX, int radiusY, ushort type, ushort preserve)
	{
		long changed = 0;
		long limit = (long)radiusX * radiusX * radiusY * radiusY;
		for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
		for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
		{
			if (x < 1 || x >= plan.Width - 1 || y < 1 || y >= plan.Height - 1)
				continue;
			long dx = x - centerX;
			long dy = y - centerY;
			if (dx * dx * radiusY * radiusY + dy * dy * radiusX * radiusX > limit ||
				Main.tile[x, y].TileType == preserve) continue;
			changed += Set(plan, x, y, type);
		}
		return changed;
	}

	private static long Set(WorldCompilerPlan plan, int x, int y, ushort type)
	{
		if (x < 1 || x >= plan.Width - 1 || y < 1 || y >= plan.Height - 1)
			return 0;
		Tile tile = Main.tile[x, y];
		tile.ClearEverything();
		tile.HasTile = true;
		tile.TileType = type;
		return 1;
	}
}
