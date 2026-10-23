using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class CalamityAbyssBufferScaler
{
	private static FieldInfo positions;

	internal static void Bind(Type abyssType)
	{
		positions = abyssType.GetField("AbyssIslandPositions",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new MissingFieldException(abyssType.FullName, "AbyssIslandPositions");
		if (positions.FieldType != typeof(Point[]))
			throw new InvalidOperationException(
				$"Calamity AbyssIslandPositions type changed to {positions.FieldType}.");
	}

	internal static void Ensure()
	{
		if (!ContinentalAdapterMath.Active()) return;
		if (positions == null)
			throw new InvalidOperationException("Calamity abyss buffer scaler is not bound.");
		int capacity = ContinentalScaleProfile.Capacity(20,
			ContinentalScaleProfile.HeightScale(Main.maxTilesY));
		Point[] values = (Point[])(positions.GetValue(null) ??
			throw new InvalidOperationException("Calamity AbyssIslandPositions is null."));
		ContinentalArrayCapacity.Grow(ref values, capacity);
		positions.SetValue(null, values);
		ContinentalWorldgenScaleReceipt.MarkAbyss(capacity);
	}

	internal static void Reset() => positions = null;
}
