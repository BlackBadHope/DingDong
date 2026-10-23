using System;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal readonly struct ContinentalDungeonWorkBudget
{
	private const int PrimaryHallWidthDivisor = 60;
	private const int PitTrapAttemptsPerWidth = 2;
	private const int WallFeatureWidthDivisor = 100;
	private const int TrapTargetWidthDivisor = 500;
	private const int LightTargetWidthDivisor = 150;
	private const int PlatformTargetWidthDivisor = 20;
	private const double GroundFurnitureAttemptsPerReferenceWidth = 2000d;
	private const double GroundFurnitureReferenceWidth = 4200d;
	private const int MinimumTilesPerStairSegment = 10;
	private const int StairSegmentSafetyMargin = 64;

	internal double PrimaryHallBase { get; }
	internal int PitTrapAttempts { get; }
	internal int WallFeatureTarget { get; }
	internal int TrapTarget { get; }
	internal int LightTarget { get; }
	internal int PlatformTarget { get; }
	internal double GroundFurnitureAttempts { get; }
	internal int StairSegmentAttempts { get; }

	private ContinentalDungeonWorkBudget(double primaryHallBase, int pitTrapAttempts,
		int wallFeatureTarget, int trapTarget, int lightTarget, int platformTarget,
		double groundFurnitureAttempts, int stairSegmentAttempts)
	{
		PrimaryHallBase = primaryHallBase;
		PitTrapAttempts = pitTrapAttempts;
		WallFeatureTarget = wallFeatureTarget;
		TrapTarget = trapTarget;
		LightTarget = lightTarget;
		PlatformTarget = platformTarget;
		GroundFurnitureAttempts = groundFurnitureAttempts;
		StairSegmentAttempts = stairSegmentAttempts;
	}

	internal static ContinentalDungeonWorkBudget Create(int worldWidth, int referenceWidth,
		int referenceHeight)
	{
		if (worldWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(worldWidth));
		if (referenceWidth <= 0)
			throw new ArgumentOutOfRangeException(nameof(referenceWidth));
		if (referenceHeight <= 0)
			throw new ArgumentOutOfRangeException(nameof(referenceHeight));
		int localWidth = Math.Min(worldWidth, referenceWidth);
		return new ContinentalDungeonWorkBudget(
			localWidth / (double)PrimaryHallWidthDivisor,
			checked(localWidth * PitTrapAttemptsPerWidth),
			localWidth / WallFeatureWidthDivisor,
			localWidth / TrapTargetWidthDivisor,
			localWidth / LightTargetWidthDivisor,
			localWidth / PlatformTargetWidthDivisor,
			GroundFurnitureAttemptsPerReferenceWidth * localWidth /
			GroundFurnitureReferenceWidth,
			checked((referenceHeight + MinimumTilesPerStairSegment - 1) /
				MinimumTilesPerStairSegment + StairSegmentSafetyMargin));
	}

	// Native geometry follows world width, not the historical Large safety cap.
	// Keep the legacy policy available only for the experimental schematic backend.
	internal static ContinentalDungeonWorkBudget CreateNative(int width, int height)
	{
		var scale = new NativeWorldScale(width, height);
		return new ContinentalDungeonWorkBudget(
			width / PrimaryHallWidthDivisor,
			checked(width * PitTrapAttemptsPerWidth),
			width / WallFeatureWidthDivisor,
			width / TrapTargetWidthDivisor,
			width / LightTargetWidthDivisor,
			width / PlatformTargetWidthDivisor,
			GroundFurnitureAttemptsPerReferenceWidth * scale.WidthFactor,
			checked(height + StairSegmentSafetyMargin));
	}
}
