using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ContinentalVanillaBufferScaler
{
	private static readonly FieldInfo HeartPositions =
		typeof(WorldGen).GetField("heartPos", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new MissingFieldException("Terraria.WorldGen", "heartPos");

	internal static void Ensure()
	{
		if (!ContinentalAdapterMath.Active()) return;
		ContinentalVanillaBufferPlan plan =
			ContinentalVanillaBufferPlan.Create(Main.maxTilesX, Main.maxTilesY);
		ContinentalArrayCapacity.Grow(ref GenVars.larvaX, plan.LarvaCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.larvaY, plan.LarvaCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.JChestX, plan.JungleChestCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.JChestY, plan.JungleChestCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.skyLake, plan.SkyFeatureCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.floatingIslandHouseX, plan.SkyFeatureCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.floatingIslandHouseY, plan.SkyFeatureCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.floatingIslandStyle, plan.SkyFeatureCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.mCaveX, plan.MountainCaveCapacity);
		ContinentalArrayCapacity.Grow(ref GenVars.mCaveY, plan.MountainCaveCapacity);
		GrowHeartPositions(plan.EvilHeartCapacity);
		ContinentalWorldgenScaleReceipt.MarkBuffers(plan);
	}

	private static void GrowHeartPositions(int capacity)
	{
		if (HeartPositions.FieldType != typeof(Point[]))
			throw new InvalidOperationException(
				$"Terraria.WorldGen.heartPos type changed to {HeartPositions.FieldType}.");
		Point[] values = (Point[])(HeartPositions.GetValue(null) ??
			throw new InvalidOperationException("Terraria.WorldGen.heartPos is null."));
		ContinentalArrayCapacity.Grow(ref values, capacity);
		HeartPositions.SetValue(null, values);
	}
}
