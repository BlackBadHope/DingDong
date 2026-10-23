using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal readonly struct ContinentalMushroomWorkBudget
{
	// Vanilla permits width / 2 probes for every requested mushroom biome.
	// Each probe scans a 200 x 200 exclusion square, so a continental width
	// turns unlucky placement into billions of tile reads without adding content.
	private const int MaximumPlacementAttempts = 128;

	internal int AttemptsPerBiome { get; }

	private ContinentalMushroomWorkBudget(int attemptsPerBiome) =>
		AttemptsPerBiome = attemptsPerBiome;

	internal static ContinentalMushroomWorkBudget Create(int worldWidth,
		int referenceWidth)
	{
		if (worldWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(worldWidth));
		if (referenceWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(referenceWidth));
		int vanillaAttempts = Math.Min(worldWidth, referenceWidth) / 2;
		return new ContinentalMushroomWorkBudget(
			Math.Min(vanillaAttempts, MaximumPlacementAttempts));
	}

	internal static ContinentalMushroomWorkBudget CreateNative(int worldWidth)
	{
		if (worldWidth <= 0) throw new ArgumentOutOfRangeException(nameof(worldWidth));
		return new ContinentalMushroomWorkBudget(worldWidth / 2);
	}
}
