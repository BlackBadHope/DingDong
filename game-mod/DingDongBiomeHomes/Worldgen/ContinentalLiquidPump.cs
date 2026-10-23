using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalLiquidPump
{
	internal const int MaximumAttempts = 4096;
	internal const double MaximumMilliseconds = 2;

	// A seam used by both the runtime and deterministic offline checks. Check
	// capacity BEFORE selecting/removing an entry or clearing checkingLiquid.
	internal static int Run(Func<bool> hasSpace, Func<bool> withinBudget,
		Func<bool> takeBuffered, Func<bool> recheckOverflow)
	{
		int attempts = 0;
		while (attempts < MaximumAttempts && hasSpace() && withinBudget())
		{
			bool progress;
			if ((attempts & 3) == 3)
				progress = recheckOverflow() || takeBuffered();
			else
				progress = takeBuffered() || recheckOverflow();
			if (!progress) break;
			attempts++;
		}
		return attempts;
	}
}
