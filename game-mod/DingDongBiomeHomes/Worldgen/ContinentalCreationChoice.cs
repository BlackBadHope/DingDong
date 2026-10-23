using System;

namespace DingDongBiomeHomes.Worldgen;

internal static class ContinentalCreationChoice
{
	internal static bool ShouldPromote(bool enabled, bool dedicated, bool menuRequested, string serverSize)
	{
		if (!enabled) return false;
		if (!dedicated) return menuRequested;
		// Enabling support is not a request to replace every server Large world.
		if (string.IsNullOrWhiteSpace(serverSize) || serverSize.Equals("large", StringComparison.OrdinalIgnoreCase)) return false;
		if (serverSize.Equals("x10", StringComparison.OrdinalIgnoreCase)) return true;
		throw new ArgumentException("DINGDONG_SERVER_WORLD_SIZE must be large or x10.");
	}
}
