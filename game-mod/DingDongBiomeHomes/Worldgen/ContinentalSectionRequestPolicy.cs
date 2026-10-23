using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalSectionRequestPolicy
{
	internal const int MaximumSectionDistance = 2;
	internal const ulong MinimumRequestInterval = 30;
	internal const ulong MinimumUrgentRequestInterval = 6;

	internal static bool IsAllowed(int sectionX, int sectionY, int currentX,
		int currentY, int sectionWidth, int sectionHeight, ulong now, ulong lastRequest,
		bool urgent = false)
	{
		if (sectionX < 0 || sectionY < 0 || sectionX >= sectionWidth ||
			sectionY >= sectionHeight)
			return false;
		if (Math.Abs(sectionX - currentX) > MaximumSectionDistance ||
			Math.Abs(sectionY - currentY) > MaximumSectionDistance)
			return false;
		ulong interval = urgent ? MinimumUrgentRequestInterval : MinimumRequestInterval;
		return lastRequest == 0 || now >= lastRequest && now - lastRequest >= interval;
	}
}
