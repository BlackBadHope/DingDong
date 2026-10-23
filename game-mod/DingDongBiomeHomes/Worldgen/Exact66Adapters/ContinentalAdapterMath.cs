using System;
using Terraria;
using Terraria.WorldBuilding;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ContinentalAdapterMath
{
	private const int VanillaLargeTiles = 8400 * 2400;
	private const int VanillaLargeAttemptBudget = VanillaLargeTiles / 5;
	internal const int SotsOreAttemptBudget = VanillaLargeAttemptBudget * 5;
	internal const int SotsGeodeAttemptBudget = VanillaLargeAttemptBudget * 2;

	internal static bool Active() => ContinentalWorldDimensions.IsContinental;

	internal static float ScaleSotsOreTarget(float target) =>
		Active() ? SotsResourceScale.BeyondLarge(target, Main.maxTilesX) : target;

	internal static int ScaleSotsGeodeTarget(int target) => Active()
		? SotsResourceScale.BeyondLarge(target, Main.maxTilesX) : target;

	internal static int BoundSotsOreAttempts(int attempts) =>
		Active() ? Math.Min(attempts, SotsOreAttemptBudget) : attempts;

	internal static int BoundSotsGeodeAttempts(int attempts) =>
		Active() ? Math.Min(attempts, SotsGeodeAttemptBudget) : attempts;

	internal static int RemapSotsOreX(int sampledX, int y)
	{
		if (!Active()) return sampledX;
		int[] minimum = GenVars.snowMinX;
		int[] maximum = GenVars.snowMaxX;
		if (minimum is null || maximum is null || y < 0 ||
			y >= minimum.Length || y >= maximum.Length) return sampledX;
		int left = minimum[y], right = maximum[y];
		if (left < 100 || right >= Main.maxTilesX - 100 || left > right) return sampledX;
		return left + (sampledX - 100) % (right - left + 1);
	}

	internal static float ScaleThoriumRadius(float radius)
	{
		if (!Active()) return radius;
		// The provider's 130-tile brush is local geometry at every supported size.
		// Only the retired schematic backend stretches that brush.
		if (!Compiler.Runtime.WorldCompilerActivation.IsActive)
		{
			ThoriumWorldgenReceipt.MarkRadius();
			return radius;
		}
		float scaled = radius * MathF.Sqrt(
			(float)ContinentalWorldDimensions.TileCount / VanillaLargeTiles);
		ThoriumWorldgenReceipt.MarkRadius();
		return scaled;
	}

	internal static float CalamitySunkenSeaMaximum(float maximum)
	{
		if (!Active()) return maximum;
		// This clamp controls the provider's local biome geometry, not how many
		// independent POIs the world receives. Stretching it with world width
		// produced map-scale domes in continental worlds.
		CalamityWorldgenReceipt.MarkScale();
		return maximum;
	}
}
