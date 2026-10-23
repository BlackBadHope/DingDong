namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenReceipt
{
	internal static string Value =>
		$"sots:ores={(OresComplete ? "complete" : "pending")},remaining={oreRemaining};" +
		$"geodes={(GeodesComplete ? "complete" : "pending")},remaining={geodeRemaining};" +
		$"topaz={(TopazComplete ? "complete" : "pending")};" +
		$"sapphire={(SapphireComplete ? "complete" : "pending")};" +
		$"pyramid={(PyramidComplete ? PyramidProfileWidth.ToString() : "pending")};" +
		$"evilBiome={(EvilBiomeGuardObserved ?
			(EvilBiomeFlattened ? "flattened" : "absent-safe") : "pending")}," +
		$"scan={(EvilBiomeScanScaled ? "scaled" :
			(CompilerEvilBridgeObserved ? "compiler-supplied" : "pending"))};" +
		$"mines={(AbandonedMineGuardObserved ?
			(AbandonedMineGenerated ? "generated" : "absent-safe") : "pending")}," +
		$"index={AbandonedMineIndex},count={CorruptionCount};" +
		$"compilerEvil={(CompilerEvilBridgeObserved ? "bridged" : "native")};" +
		$"villageLoot={(AbandonedVillageLootGuardObserved ?
			(AbandonedVillageLootProcessed ? "processed" : "absent-safe") : "pending")};" +
		SanctuaryValue;
}
