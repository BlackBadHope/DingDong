namespace DingDongBiomeHomes.Worldgen;

internal static class UnderworldCandidateDomain
{
	internal static bool TryVerticalRange(int worldHeight, int biomeDepth,
		int bottomClearance, int sceneHeight, out int minimum, out int maximum)
	{
		// The roof allowance is local scene clearance, not a biome depth.
		minimum = worldHeight - biomeDepth - 230;
		maximum = worldHeight - bottomClearance - sceneHeight;
		return sceneHeight > 0 && biomeDepth > 0 && bottomClearance >= 0 &&
			minimum >= 1 && maximum >= minimum;
	}
}
