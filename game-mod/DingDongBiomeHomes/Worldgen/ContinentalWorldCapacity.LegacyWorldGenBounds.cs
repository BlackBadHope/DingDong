using System;
using System.Reflection;
using Terraria;

namespace DingDongBiomeHomes.Worldgen;

internal static partial class ContinentalWorldCapacity
{
	private static FieldInfo worldGenLastMaxTilesX;
	private static FieldInfo worldGenLastMaxTilesY;

	private static void InitializeLegacyWorldGenBounds()
	{
		worldGenLastMaxTilesX = RequireWorldGenDimensionField("lastMaxTilesX");
		worldGenLastMaxTilesY = RequireWorldGenDimensionField("lastMaxTilesY");
	}

	private static void ResetLegacyWorldGenBounds()
	{
		worldGenLastMaxTilesX = null;
		worldGenLastMaxTilesY = null;
	}

	private static FieldInfo RequireWorldGenDimensionField(string name) =>
		typeof(WorldGen).GetField(name, BindingFlags.NonPublic | BindingFlags.Static) ??
		throw new MissingFieldException(typeof(WorldGen).FullName, name);

	private static void SynchronizeLegacyWorldGenBounds()
	{
		if (Main.maxTilesX <= 0 || Main.maxTilesY <= 0)
			return;
		worldGenLastMaxTilesX.SetValue(null, Main.maxTilesX);
		worldGenLastMaxTilesY.SetValue(null, Main.maxTilesY);
		int cachedX = (int)worldGenLastMaxTilesX.GetValue(null)!;
		int cachedY = (int)worldGenLastMaxTilesY.GetValue(null)!;
		if (cachedX != Main.maxTilesX || cachedY != Main.maxTilesY)
			throw new InvalidOperationException(
				$"Legacy WorldGen bounds did not synchronize: {cachedX}x{cachedY} != {Main.maxTilesX}x{Main.maxTilesY}.");
	}
}
