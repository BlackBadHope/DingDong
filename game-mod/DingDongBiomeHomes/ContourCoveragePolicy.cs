using System;

namespace DingDongBiomeHomes;

internal static class ContourCoveragePolicy
{
	internal const int MinimumBiomeCoveragePercent = 60;
	internal const int MaximumForeignSolidPercent = 25;
	internal const int MaximumEvilSolidPercent = 5;

	internal static bool HasMinimumCoverage(int accepted, int total, int minimumPercent)
		=> total > 0 && accepted >= 0 && accepted <= total &&
			(long)accepted * 100 >= (long)total * minimumPercent;

	internal static bool IsWithinRejectionBudget(int rejected, int total,
		int maximumPercent, int minimumAllowance)
	{
		if (rejected < 0 || total < 0 || rejected > total)
			return false;
		int budget = Math.Max(minimumAllowance, total * maximumPercent / 100);
		return rejected <= budget;
	}
}
