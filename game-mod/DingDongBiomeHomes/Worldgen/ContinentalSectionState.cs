using System;
using System.Reflection;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalSectionState
{
	// WorldSections has no public capacity properties in the pinned engine.
	// Read its dimensions so a correctly sized vanilla clearWorld instance
	// can be retained too; replacing it would lose already received tiles.
	private static readonly FieldInfo WidthField = DimensionField("width");
	private static readonly FieldInfo HeightField = DimensionField("height");

	internal static WorldSections EnsureWorldSections(WorldSections current,
		int width, int height)
	{
		if (current != null && (int)WidthField.GetValue(current) >= width &&
			(int)HeightField.GetValue(current) >= height)
			return current;
		return new WorldSections(width, height);
	}

	internal static bool[,] EnsureRemoteSections(bool[,] current, int width, int height) =>
		current != null && current.GetLength(0) >= width && current.GetLength(1) >= height
			? current : new bool[width, height];

	private static FieldInfo DimensionField(string name) =>
		typeof(WorldSections).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new MissingFieldException(typeof(WorldSections).FullName, name);
}
