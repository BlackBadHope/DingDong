using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalSectionGuardPolicy
{
	internal static bool RebaseAfterDiscontinuity(float dx, float dy, float speed)
	{
		float limit = Math.Max(256f, speed * 8f + 96f);
		return dx * dx + dy * dy > limit * limit;
	}
	internal static bool RequestAt(int stalledTicks) => stalledTicks == 1 || stalledTicks % 30 == 0;
}
