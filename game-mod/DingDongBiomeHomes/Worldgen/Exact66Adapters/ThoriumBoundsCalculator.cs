using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ThoriumBoundsCalculator
{
	private const int MaximumMorphTiles = 8_000_000;
	private static FieldInfo morphField;
	private static FieldInfo stoneOneField;
	private static FieldInfo stoneTwoField;
	private static FieldInfo boundsField;

	internal static void Bind(Type passType)
	{
		BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
		morphField = RequireField(passType, "_tilesToMorph", instance, typeof(HashSet<Point>));
		stoneOneField = RequireField(passType, "_tileID_BasicStone1", instance, typeof(ushort));
		stoneTwoField = RequireField(passType, "_tileID_BasicStone2", instance, typeof(ushort));
		Type system = passType.Assembly.GetType("ThoriumMod.WorldGeneration.WorldGenerationSystem", true);
		boundsField = RequireField(system, "AquaticDepthsBounds",
			BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, typeof(Rectangle));
	}

	internal static void Reset()
	{
		morphField = null;
		stoneOneField = null;
		stoneTwoField = null;
		boundsField = null;
	}

	internal static void Calculate(object pass)
	{
		HashSet<Point> points = (HashSet<Point>)(morphField?.GetValue(pass) ??
			throw new InvalidOperationException("Thorium morph set is unbound."));
		if (points.Count == 0)
			throw new InvalidOperationException("Thorium Aquatic Depths morph set is empty.");
		if (points.Count > MaximumMorphTiles)
			throw new InvalidOperationException(
				$"Thorium Aquatic Depths morph set exceeded its bounded budget: {points.Count}.");
		ushort stoneOne = ReadStone(stoneOneField, pass);
		ushort stoneTwo = ReadStone(stoneTwoField, pass);
		FindActualBounds(points, stoneOne, stoneTwo,
			out int foundLeft, out int foundRight, out int foundTop, out int foundBottom);
		if (foundLeft < 0)
			throw new InvalidOperationException("Thorium Aquatic Depths generated no canonical stone tiles.");
		boundsField.SetValue(null, new Rectangle(foundLeft, foundTop,
			foundRight - foundLeft, foundBottom - foundTop));
		ThoriumWorldgenReceipt.MarkBounds();
	}

	private static void FindActualBounds(IEnumerable<Point> points,
		ushort one, ushort two, out int minX, out int maxX, out int minY, out int maxY)
	{
		minX = minY = -1; maxX = maxY = -1;
		foreach (Point point in points)
		{
			if (!WorldGen.InWorld(point.X, point.Y)) continue;
			Tile tile = Main.tile[point.X, point.Y];
			if (!tile.HasTile || (tile.TileType != one && tile.TileType != two)) continue;
			if (minX < 0) { minX = maxX = point.X; minY = maxY = point.Y; continue; }
			minX = Math.Min(minX, point.X); maxX = Math.Max(maxX, point.X);
			minY = Math.Min(minY, point.Y); maxY = Math.Max(maxY, point.Y);
		}
	}

	private static ushort ReadStone(FieldInfo field, object pass) =>
		(ushort)(field?.GetValue(pass) ?? throw new InvalidOperationException("Thorium stone ID is unbound."));

	private static FieldInfo RequireField(Type type, string name, BindingFlags flags, Type expected)
	{
		FieldInfo field = type.GetField(name, flags);
		if (field is null || field.FieldType != expected) throw new MissingFieldException(type.FullName, name);
		return field;
	}
}
