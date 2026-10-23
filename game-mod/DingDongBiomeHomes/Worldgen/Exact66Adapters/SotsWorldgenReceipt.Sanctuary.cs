namespace DingDongBiomeHomes.Worldgen.Exact66Adapters;

internal static partial class SotsWorldgenReceipt
{
	internal static bool SanctuaryBottomObserved { get; private set; }
	internal static bool SanctuaryGuardObserved { get; private set; }
	internal static int SanctuaryBottom { get; private set; }
	internal static int SanctuaryRejectedEdges { get; private set; }
	private static string SanctuaryValue =>
		$"sanctuary={(SanctuaryBottomObserved && SanctuaryGuardObserved ? "bounded" : "pending")}," +
		$"bottom={SanctuaryBottom},rejectedEdges={SanctuaryRejectedEdges}";

	internal static void MarkSanctuaryBottom(int bottom)
	{
		SanctuaryBottomObserved = true;
		SanctuaryBottom = bottom;
	}

	internal static void MarkSanctuaryGuard(bool admitted)
	{
		SanctuaryGuardObserved = true;
		if (!admitted) SanctuaryRejectedEdges++;
	}

	private static void ResetSanctuary()
	{
		SanctuaryBottomObserved = SanctuaryGuardObserved = false;
		SanctuaryBottom = SanctuaryRejectedEdges = 0;
	}
}
