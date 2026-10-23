using System;
using DingDongBiomeHomes.Worldgen.Compiler.Runtime;

namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenReceipt
{
	private static float oreRemaining = float.NaN;
	private static int geodeRemaining = -1;
	internal static bool OresComplete { get; private set; }
	internal static bool GeodesComplete { get; private set; }
	internal static bool TopazComplete { get; private set; }
	internal static bool SapphireComplete { get; private set; }
	internal static bool PyramidComplete { get; private set; }
	internal static bool EvilBiomeGuardObserved { get; private set; }
	internal static bool EvilBiomeFlattened { get; private set; }
	internal static bool EvilBiomeScanScaled { get; private set; }
	internal static bool AbandonedMineGuardObserved { get; private set; }
	internal static bool AbandonedMineGenerated { get; private set; }
	internal static int AbandonedMineIndex { get; private set; }
	internal static int CorruptionCount { get; private set; }
	internal static int PyramidProfileWidth { get; private set; }
	internal static bool Complete =>
		OresComplete && GeodesComplete && TopazComplete && SapphireComplete &&
		PyramidComplete && EvilBiomeGuardObserved && EvilBiomeScanScaled &&
		AbandonedMineGuardObserved && AbandonedVillageLootGuardObserved &&
		SanctuaryBottomObserved && SanctuaryGuardObserved &&
		(!WorldCompilerActivation.IsActive || CompilerEvilBridgeObserved &&
		AbandonedMineGenerated && AbandonedVillageLootProcessed);
	internal static bool CompilerComplete =>
		OresComplete && GeodesComplete && TopazComplete && SapphireComplete &&
		PyramidComplete && EvilBiomeGuardObserved && CompilerEvilBridgeObserved &&
		AbandonedMineGuardObserved && AbandonedMineGenerated &&
		AbandonedVillageLootGuardObserved && AbandonedVillageLootProcessed &&
		SanctuaryBottomObserved && SanctuaryGuardObserved;
	internal static void ValidateOres(float remaining)
	{
		if (!ContinentalAdapterMath.Active()) { OresComplete = false; oreRemaining = float.NaN; return; }
		oreRemaining = Math.Max(0f, remaining);
		OresComplete = remaining <= 0f;
		if (!OresComplete)
			throw new InvalidOperationException(
				$"SOTS ore target missed by {remaining} after bounded budget " +
				$"{ContinentalAdapterMath.SotsOreAttemptBudget}.");
	}

	internal static void ValidateGeodes(int remaining)
	{
		if (!ContinentalAdapterMath.Active()) { GeodesComplete = false; geodeRemaining = -1; return; }
		geodeRemaining = Math.Max(0, remaining);
		GeodesComplete = remaining <= 0;
		if (!GeodesComplete)
			throw new InvalidOperationException(
				$"SOTS geode target missed by {remaining} after bounded budget " +
				$"{ContinentalAdapterMath.SotsGeodeAttemptBudget}.");
	}

	internal static void MarkTopaz() => TopazComplete = true;

	internal static void MarkSapphire() => SapphireComplete = true;
	internal static void MarkEvilScan() => EvilBiomeScanScaled = true;
	internal static void MarkEvilBiomeGuard(bool flattened)
	{
		EvilBiomeGuardObserved = true;
		EvilBiomeFlattened = flattened;
	}
	internal static void MarkAbandonedMineGuard(bool generated, int index, int count)
	{
		AbandonedMineGuardObserved = true;
		AbandonedMineGenerated = generated;
		AbandonedMineIndex = index;
		CorruptionCount = count;
	}
	internal static void MarkPyramid(int profileWidth)
	{
		PyramidProfileWidth = profileWidth;
		// This receipt observes the width hook, not physical pyramid integrity.
		PyramidComplete = profileWidth ==
			ContinentalStructureWorkBudget.BoundSotsLegacyWidth(Terraria.Main.maxTilesX);
	}

	internal static void Reset()
	{
		OresComplete = GeodesComplete = TopazComplete = SapphireComplete = PyramidComplete =
		EvilBiomeGuardObserved = EvilBiomeFlattened = false;
		EvilBiomeScanScaled = false;
		AbandonedMineGuardObserved = AbandonedMineGenerated = false;
		ResetAbandonedVillage();
		oreRemaining = float.NaN;
		geodeRemaining = -1;
		PyramidProfileWidth = 0;
		AbandonedMineIndex = -1;
		CorruptionCount = 0;
		ResetSanctuary();
	}
}
