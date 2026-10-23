using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal readonly struct ContinentalDesertWorkBudget
{
	// The first two search windows preserve both vanilla sides.  If neither side
	// admits the structure, vanilla already switches skipDesertTileCheck on and
	// retries placement.  Thousands of pre-fallback probes add no extra biome;
	// on a continental tile map they only multiply the expensive occupancy test.
	private const int MaximumProbesPerSide = 128;

	internal int AttemptsPerSide { get; }

	private ContinentalDesertWorkBudget(int attemptsPerSide) =>
		AttemptsPerSide = attemptsPerSide;

	internal static ContinentalDesertWorkBudget Create(int worldWidth, int referenceWidth)
	{
		if (worldWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(worldWidth));
		if (referenceWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(referenceWidth));
		int vanillaAttempts = Math.Min(worldWidth, referenceWidth) / 4;
		return new ContinentalDesertWorkBudget(
			Math.Min(vanillaAttempts, MaximumProbesPerSide));
	}

	internal static ContinentalDesertWorkBudget CreateNative(int worldWidth)
	{
		if (worldWidth <= 0) throw new ArgumentOutOfRangeException(nameof(worldWidth));
		return new ContinentalDesertWorkBudget(worldWidth / 4);
	}
}
