using System;

namespace DingDongBiomeHomes.Worldgen;

// Carry compression overruns forward instead of granting a fresh full budget
// on the next tick. A native SendSection call itself cannot be interrupted.
internal sealed class ContinentalSectionDispatchBudget
{
	internal const double MaximumBurstMilliseconds = 4;
	private double credit;
	private ulong lastTick;
	private bool initialized;

	internal void Refill(ulong tick, int activePlayers)
	{
		if (activePlayers <= 0) { Reset(); return; }
		ulong elapsed = !initialized || tick < lastTick ? 1 : tick - lastTick;
		initialized = true;
		lastTick = tick;
		// Preserve the old nominal 4 ms / player / six ticks, with a shared cap.
		double perTick = Math.Min(MaximumBurstMilliseconds,
			activePlayers * ContinentalSectionStreamingSystem.SendTimeBudgetMilliseconds /
			ContinentalSectionStreamingSystem.PrefetchInterval);
		credit = Math.Min(MaximumBurstMilliseconds,
			credit + Math.Min(elapsed, 6UL) * perTick);
	}

	internal double Available(double elapsedThisTick) =>
		Math.Max(0, Math.Min(credit, MaximumBurstMilliseconds - elapsedThisTick));

	internal void Charge(double milliseconds) => credit -= Math.Max(0, milliseconds);

	internal void Reset()
	{
		credit = 0;
		lastTick = 0;
		initialized = false;
	}
}
