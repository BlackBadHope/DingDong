using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ThoriumOceanFloodFill
{
	private const int WorldMargin = 2;
	private static FieldInfo scannedField;
	private static FieldInfo expandableField;

	internal static void Bind(Type passType)
	{
		BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
		scannedField = RequireField(passType, "_tilesOceanScanned", fields, typeof(HashSet<Point>));
		expandableField = RequireField(passType, "_biomeCanExpandOver", fields, typeof(bool[]));
	}

	internal static void Reset()
	{
		scannedField = null;
		expandableField = null;
	}

	internal static void Scan(object pass, int startX, int startY)
	{
		if (!WorldGen.InWorld(startX, startY))
			return;
		HashSet<Point> ocean = (HashSet<Point>)(scannedField?.GetValue(pass) ??
			throw new InvalidOperationException("Thorium ocean scan set is unbound."));
		bool[] expandable = (bool[])(expandableField?.GetValue(pass) ??
			throw new InvalidOperationException("Thorium ocean expansion lookup is unbound."));
		int worldWidth = Main.maxTilesX;
		int tileCount = checked(worldWidth * Main.maxTilesY);
		BitArray scheduled = new(tileCount);
		Queue<Point> frontier = new();
		long examined = 0;
		Enqueue(startX, startY, worldWidth, scheduled, frontier);

		while (frontier.TryDequeue(out Point point))
		{
			examined++;
			if (ocean.Contains(point))
				continue;
			Tile tile = Main.tile[point.X, point.Y];
			bool passable = tile.HasTile
				? tile.TileType < expandable.Length && expandable[tile.TileType]
				: tile.LiquidAmount >= 1;
			if (!passable)
				continue;
			ocean.Add(point);
			Enqueue(point.X - 1, point.Y, worldWidth, scheduled, frontier);
			Enqueue(point.X + 1, point.Y, worldWidth, scheduled, frontier);
			Enqueue(point.X, point.Y + 1, worldWidth, scheduled, frontier);
		}

		ThoriumWorldgenReceipt.MarkOceanScan(examined, ocean.Count);
	}

	private static void Enqueue(int x, int y, int worldWidth,
		BitArray scheduled, Queue<Point> frontier)
	{
		if (!WorldGen.InWorld(x, y, WorldMargin))
			return;
		int index = checked(y * worldWidth + x);
		if (scheduled[index])
			return;
		scheduled[index] = true;
		frontier.Enqueue(new Point(x, y));
	}

	private static FieldInfo RequireField(Type type, string name, BindingFlags flags, Type expected)
	{
		FieldInfo field = type.GetField(name, flags);
		if (field is null || field.FieldType != expected)
			throw new MissingFieldException(type.FullName, name);
		return field;
	}
}
