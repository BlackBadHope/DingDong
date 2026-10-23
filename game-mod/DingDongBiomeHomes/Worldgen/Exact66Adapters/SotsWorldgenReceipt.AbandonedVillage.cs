namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenReceipt
{
	internal static bool CompilerEvilBridgeObserved { get; private set; }
	internal static bool AbandonedVillageLootGuardObserved { get; private set; }
	internal static bool AbandonedVillageLootProcessed { get; private set; }

	internal static void MarkCompilerEvilBridge() =>
		CompilerEvilBridgeObserved = true;

	internal static void MarkAbandonedVillageLootGuard(bool processed)
	{
		AbandonedVillageLootGuardObserved = true;
		AbandonedVillageLootProcessed = processed;
	}

	private static void ResetAbandonedVillage()
	{
		CompilerEvilBridgeObserved = false;
		AbandonedVillageLootGuardObserved = false;
		AbandonedVillageLootProcessed = false;
	}
}
