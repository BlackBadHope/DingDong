namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static class ThoriumWorldgenReceipt
{
	internal static bool RadiusObserved { get; private set; }
	internal static bool BoundsComplete { get; private set; }
	internal static bool OceanScanOptimized { get; private set; }
	internal static long OceanTilesExamined { get; private set; }
	internal static int OceanTilesAccepted { get; private set; }
	internal static bool Complete => RadiusObserved && BoundsComplete && OceanScanOptimized;
	internal static string Value =>
		$"thorium:radius={(RadiusObserved ? "observed" : "pending")};" +
		$"bounds={(BoundsComplete ? "complete" : "pending")};" +
		$"ocean={(OceanScanOptimized ? $"linear/{OceanTilesExamined}/{OceanTilesAccepted}" : "pending")}";

	internal static void MarkRadius() => RadiusObserved = true;

	internal static void MarkBounds() => BoundsComplete = true;

	internal static void MarkOceanScan(long examined, int accepted)
	{
		OceanTilesExamined = examined;
		OceanTilesAccepted = accepted;
		OceanScanOptimized = true;
	}

	internal static void Reset()
	{
		RadiusObserved = BoundsComplete = OceanScanOptimized = false;
		OceanTilesExamined = OceanTilesAccepted = 0;
	}
}
